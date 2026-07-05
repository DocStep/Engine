using System;
using System.Numerics;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.Collision;

namespace Engine;


public static class PhysicsUtils {

    extension(Raycast) {
        public static bool RaycastSceneCollider (World world, Ray ray, out RigidBody? hitBody, out Vector3 hitPoint, out Vector3 hitNormal, out float fraction) {
            hitBody = null;
            hitPoint = default;
            hitNormal = default;
            fraction = 0f;

            JVector origin = new(ray.Origin.X, ray.Origin.Y, ray.Origin.Z);
            JVector dir = new(ray.Direction.X, ray.Direction.Y, ray.Direction.Z);

            bool hit = world.DynamicTree.RayCast(
                origin, dir,
                pre: null,
                post: null,
                out IDynamicTreeProxy? proxy,
                out JVector normal,
                out float lambda);

            if (!hit || proxy is not Jitter2.Collision.Shapes.RigidBodyShape shape) return false;

            hitBody = shape.RigidBody;
            fraction = (float)lambda;
            hitNormal = new Vector3((float)normal.X, (float)normal.Y, (float)normal.Z);
            hitPoint = ray.Origin + ray.Direction*fraction;
            return true;
        }
    }



}