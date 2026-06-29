using System.Numerics;

namespace Engine;

public static class Raycaster1 {

    public static (Vector3 origin, Vector3 direction) ScreenPointToRay (
        float mouseX, float mouseY, int viewportWidth, int viewportHeight,
        Matrix4x4 view, Matrix4x4 projection) {

        /// Pixels -> normalized device coordinates [-1, 1], Y flipped (screen Y is top-down).
        float ndcX = (2f*mouseX)/viewportWidth - 1f;
        float ndcY = 1f - (2f*mouseY)/viewportHeight;

        Matrix4x4.Invert(view*projection, out Matrix4x4 inverseViewProjection);

        Vector4 nearPoint4 = Vector4.Transform(new Vector4(ndcX, ndcY, -1f, 1f), inverseViewProjection);
        Vector4 farPoint4 = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), inverseViewProjection);

        Vector3 nearPoint = new Vector3(nearPoint4.X, nearPoint4.Y, nearPoint4.Z)/nearPoint4.W;
        Vector3 farPoint = new Vector3(farPoint4.X, farPoint4.Y, farPoint4.Z)/farPoint4.W;

        Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
        return (nearPoint, direction);
    }

    private static float GetAxis (Vector3 v, int axis) => axis switch {
        0 => v.X,
        1 => v.Y,
        _ => v.Z,
    };

}
