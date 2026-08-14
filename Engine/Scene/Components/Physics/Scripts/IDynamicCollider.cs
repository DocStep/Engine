using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;

namespace Engine;


public interface IDynamicCollider {
    TypedIndex AddShape (Simulation simulation, BufferPool pool);
    BodyInertia ComputeInertia (float mass);
}
