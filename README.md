# CompositionalPooling
An implementation of the object pool pattern for Unity's object-oriented framework designed to be efficient while easy to use.

Features:
- Minimal overhead and maximal throughput
- Type-compatible with Unity's Instantiate/Destroy API
- Simple to setup
- Easy to extend

Notes:
- The system can work with no setup at the cost of using normal instantiation and destruction as the fallback procedure.
- The system is setup only for the basic built-in component types by default (See MapperSetup.cs).
- The system requires no pool initialization and it will create the pools automatically as required.
- To start using the system, import 'CompositionalPooling' namespace and replace desired 'Object.Instantiate' and 'Object.Destroy' calls with respective 'PoolingSystem.Request' and 'PoolingSystem.Release' calls.

This source code is an extraction from the original asset with only the editor tools removed. It is free for personal use. You can purchase the original asset from Unity asset store. The link will be put here after the asset is released.
