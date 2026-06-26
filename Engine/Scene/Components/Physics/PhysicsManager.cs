using System;
using System.Numerics;
using System.Collections.Generic;

namespace Engine;

public static class PhysicsManager {

    public static Vector3 Gravity = new(0f, -9.81f, 0f);
    public static float bounciness = 0.1f;

    static readonly List<ColliderComponent> dynamicColliders = new();
    static readonly List<ColliderComponent> staticColliders = new();

    public static void Register (ColliderComponent c) { (c.isStatic ? staticColliders : dynamicColliders).Add(c); }
    public static void Unregister (ColliderComponent c) { (c.isStatic ? staticColliders : dynamicColliders).Remove(c); }

    public static void FixedUpdate () {
        float dt = (float)Engine.fixedDeltaTime;
        /// Integrate all dynamic bodies
        foreach (var col in dynamicColliders) {
            var phys = col.owner.GetComponent<PhysicsComponent>();
            if (phys == null || phys.isKinematic) continue;
            phys.Integrate(dt);
        }

        /// Broad-phase + narrow-phase + resolution
        /// dynamic vs static
        foreach (var dyn in dynamicColliders) {
            if (dyn is not BoxColliderComponent dynBox) continue;
            var phys = dyn.owner.GetComponent<PhysicsComponent>();

            foreach (var stat in staticColliders) {
                if (stat is not BoxColliderComponent statBox) continue;
                Manifold m = OBBCollision.Test(dynBox.GetWorldOBB(), statBox.GetWorldOBB());
                if (!m.Colliding) continue;
                if (phys != null) ResolveManifold(phys, null, m, dt);
            }
        }

        // dynamic vs dynamic
        for (int i = 0; i < dynamicColliders.Count; i++) {
            if (dynamicColliders[i] is not BoxColliderComponent boxA) continue;
            var physA = dynamicColliders[i].owner.GetComponent<PhysicsComponent>();

            for (int j = i + 1; j < dynamicColliders.Count; j++) {
                if (dynamicColliders[j] is not BoxColliderComponent boxB) continue;
                var physB = dynamicColliders[j].owner.GetComponent<PhysicsComponent>();

                Manifold m = OBBCollision.Test(boxA.GetWorldOBB(), boxB.GetWorldOBB());
                if (!m.Colliding) continue;
                ResolveManifold(physA, physB, m, dt);
            }
        }
    }

    // ── impulse resolution ────────────────────────────────────────────────────

    static void ResolveManifold (PhysicsComponent? a, PhysicsComponent? b, in Manifold m, float dt) {
        float invMassA = a != null && !a.isKinematic ? 1f / a.mass : 0f;
        float invMassB = b != null && !b.isKinematic ? 1f / b.mass : 0f;
        if (invMassA + invMassB == 0f) return;

        // Inertia tensors (uniform box approximation)
        Matrix4x4 invIA = a != null && !a.isKinematic ? BoxInvInertia(a) : Matrix4x4.Identity * 0f;
        Matrix4x4 invIB = b != null && !b.isKinematic ? BoxInvInertia(b) : Matrix4x4.Identity * 0f;

        Vector3 n = m.Normal;

        for (int i = 0; i < m.ContactCount; i++) {
            Contact c = m[i];

            Vector3 rA = a != null ? c.Point - a.owner.Transform.Position : Vector3.Zero;
            Vector3 rB = b != null ? c.Point - b.owner.Transform.Position : Vector3.Zero;

            Vector3 vA = a?.Velocity        ?? Vector3.Zero;
            Vector3 wA = a?.VelocityAngular ?? Vector3.Zero;
            Vector3 vB = b?.Velocity        ?? Vector3.Zero;
            Vector3 wB = b?.VelocityAngular ?? Vector3.Zero;

            Vector3 relVel = (vB + Vector3.Cross(wB, rB)) - (vA + Vector3.Cross(wA, rA));
            float vn = Vector3.Dot(relVel, n);

            if (vn > 0f) continue;   // separating

            // ── normal impulse ────────────────────────────────────────────────
            Vector3 rAxN = Vector3.Cross(rA, n);
            Vector3 rBxN = Vector3.Cross(rB, n);
            float angA = Vector3.Dot(rAxN, Transform3x3(invIA, rAxN));
            float angB = Vector3.Dot(rBxN, Transform3x3(invIB, rBxN));

            float e = bounciness;
            float jn = -(1f + e) * vn / ((invMassA + invMassB + angA + angB) * m.ContactCount);

            Vector3 impulse = jn * n;
            a?.ApplyImpulse(-impulse, rA);
            b?.ApplyImpulse(impulse, rB);

            // ── positional correction (Baumgarte) ────────────────────────────
            const float baumgarte = 0.2f, slop = 0.005f;
            float corr = MathF.Max(c.Penetration - slop, 0f) * baumgarte / (invMassA + invMassB);
            Vector3 corrVec = corr * n;
            if (a != null && !a.isKinematic) a.owner.Transform.Position -= corrVec * invMassA;
            if (b != null && !b.isKinematic) b.owner.Transform.Position += corrVec * invMassB;

            // ── friction impulse ──────────────────────────────────────────────
            Vector3 tangent = relVel - vn * n;
            if (tangent.LengthSquared() < 1e-8f) continue;
            tangent = Vector3.Normalize(tangent);

            float vt = Vector3.Dot(relVel, tangent);
            Vector3 rAxT = Vector3.Cross(rA, tangent);
            Vector3 rBxT = Vector3.Cross(rB, tangent);
            float angAT = Vector3.Dot(rAxT, Transform3x3(invIA, rAxT));
            float angBT = Vector3.Dot(rBxT, Transform3x3(invIB, rBxT));

            float jt = -vt / ((invMassA + invMassB + angAT + angBT) * m.ContactCount);
            float mu = 0.4f;   // combined friction coefficient — expose if needed
            jt = Utils.Clamp(jt, -MathF.Abs(jn) * mu, MathF.Abs(jn) * mu);

            Vector3 frictionImpulse = jt * tangent;
            a?.ApplyImpulse(-frictionImpulse, rA);
            b?.ApplyImpulse(frictionImpulse, rB);
        }
    }

    // ── inertia helpers ───────────────────────────────────────────────────────

    // Uniform solid box: I = m/12 * (b²+c², a²+c², a²+b²)
    static Matrix4x4 BoxInvInertia (PhysicsComponent p) {
        Vector3 s = p.owner.Transform.Scale;   // treat scale as box dimensions
        float m = p.mass;
        float ix = m / 12f * (s.Y * s.Y + s.Z * s.Z);
        float iy = m / 12f * (s.X * s.X + s.Z * s.Z);
        float iz = m / 12f * (s.X * s.X + s.Y * s.Y);
        // Return inverse (diagonal)
        return new Matrix4x4(
            ix > 0f ? 1f/ix : 0f, 0, 0, 0,
            0, iy > 0f ? 1f/iy : 0f, 0, 0,
            0, 0, iz > 0f ? 1f/iz : 0f, 0,
            0, 0, 0, 1f
        );
    }

    // Multiply a 3×3 upper-left of a Matrix4x4 by a Vector3.
    static Vector3 Transform3x3 (Matrix4x4 m, Vector3 v) => new(
        m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z,
        m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z,
        m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z
    );
}