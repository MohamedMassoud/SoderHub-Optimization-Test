# Optimization Test Document

## Index
- [Hardware and Software Specifications](#hardware-and-software-specifications)
- [Performance Goals](#performance-goals)
- [Performance Tools](#performance-tools)
- [Performance Issues](#performance-issues)
- [Optimization Options](#optimization-options)
- [Performance Optimization](#performance-optimization)
- [Post Optimization Results](#post-optimization-results)
- [Current Limitations](#current-limitations)
- [Break the Limitations](#break-the-limitations)

### Hardware and Software Specifications

Processor: Ryzen 5 2600. 6 Cores. 3.40 GHz.
RAM: 32GB 2800 MHz Overclocked to 3000 MHz XMP Dual Channel.
Graphics card: GTX 1060 6GB
OS: Win 11 V22H2
Unity: 2021.3.6f1

### Performance Goals

Running the scene on my machine with 10,000 Actors and 10,000 Max Marbles gave me around 1 – 3 FPS. That sets my goal to reach around 40 FPS if we take the average.

### Performance Tools

- Profiler: Most of the time taken is inside the ActorBehavior scripts.

### Performance Issues

- The structure of ActorBehaviors and MarbleBehaviors is not optimized: Having 10K Actors and Marbles will imply having 20K Behaviors running Update() Function.
- The way the actor searches for the nearest marble is not optimized: Ordering each actor search by Euclidean distance and then filtering by taking 10 and choosing one of them if not claimed is not optimized.
- Huge garbage collection costs. The Marbles and the TextContainers get destroyed and re-used as long the simulation is running.
- Unneeded cost of unused Physics system and colliders. Actors and Marbles had colliders, collision is not handled by Physics system.

### Optimization Options

- Had to use a different datastructure/algorithm to find nearest marble. Had a pool of options {KD Tree, R Tree, OctTree}. KD tree was the selection approach due to
  - Optimized Log(n)
  - Works in higher dimensional spaces (3D)
  - Can easily modify tree at runtime
- I wanted to use Jobs system and burst compiler. But as them being a part of the DOTs system. I didn’t use them.
- I tried to implement something like the Jobs system myself by assigning multiple find nearest neighbor algorithm on the KD-Tree for multiple actors in parallel. But after some time I figured it is out of the test’s scope.

### Performance Optimization

- Removed ActorBehavior and MarbleBehaviors. They are now only prefabs. All the logic works inside 2 Scripts. ActorContainer and MarbleContainer Singletons attached to SingletonFactory.
- Optimized Asset Loading: the 2 prefabs that we instantiate from are loaded from the hierarchy and not from the Assets to minimize overhead.
- Custom made PoolManager to pool respawnable objects and limit GC such as Marbles and TextContainers.
- Imported a KD-Tree Data structure implementation for viliwonka’s project on [Git](https://github.com/viliwonka/KDTree). The tree is built in a way to load the Vector3 Nodes so that searching for the nearest point in a 3D spatial dynamic dataset is optimized to logarithmic time. Also used the KD-Tree in an optimized way to reduce the number of times needed to rebuild the tree by only rebuilding it after bulk instantiation of Marble Vectors.
- Change target of actor when the target is claimed by another actor dynamically.
- Optimizing project settings: Disabling auto simulation of physics, removed lighting, graphics settings and disabling shadows.
- Optimized materials: used unlit materials.
- Dynamic and Static batching: Used to reduce the number of Graphics API draw calls.
- Usage of coroutine loops: to have control over how often ticks and renders of scripts are made. For example, added a delay for how often an actor searches for a nearest marble inside the Idle state.

### Post Optimization Results

- FPS hits between 40-50 FPS with no FPS drop spikes.
- FPS can hit more by adjusting how frequent bulk marbles are created {Currently set to 0.05 sec} {Currently set to 0.05 sec} and how frequently actors search for the nearest marble in Idle State. 

### Current Limitations

- **Everything runs on the main thread. nActors are still updating transforms and calculating a heavy task {Searching for Nearest Marble} on a single thread. That’s why the script is taking around 53% of the PlayerLoop time.**

### Break the Limitations

- **By using DOTS and ECS.**
- **Using DOT’s Jobs System by assigning multiple Workers that run multiple threads to do both Transform motion using IJobParallelForTransform jobs and IJobParallelFor for calculating nearest points for multiple actors at the same time.**
- **Using Burst compiler to boost the workers.**
- **Using these, It is guaranteed to boost the performance even more.**
