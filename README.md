# Post Mortem...
The Rust implementation (GDExtension) for this OctTree was unfortunately lost during a system migration, but the video below demonstrates the performance achieved (100k boids at 60fps).
https://www.youtube.com/watch?v=NAF7I6-K-Vo

## Lessons Learned..
- Language doesn't matter as much as architecture.
- Flat arrays and uid/Rid PhysicsServer/RenderServer are the "closest to the metal" you can get in godot.
- Godot-Rust is really great for computation loops, bad if you wanna use the engine for much of anything. IE in engine bvh isn't worth trying to setup...
