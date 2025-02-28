---
outputFileName: index.html
---

# Kepware Configuration API SDK for .NET

[![Build Status](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions)
[![Build Status](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions/workflows/docker-build-and-push.yml/badge.svg)](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions)

## Overview
The Kepware Configuration API SDK for .NET provides tools and libraries to interact with the Kepware Configuration REST API, enabling configuration management for Kepware servers. This repository includes examples and utilities to streamline development for deployment tools, including a service for continuous synchronization and an API client library. 

This package is designed to work with all versions of Kepware that support the Configuration API including Thingworx Kepware Server (TKS), Thingworx Kepware Edge (TKE) and KEPServerEX (KEP). For reference, Kepware Server in this documentation will refer to both TKS and KEP versions.

## Features
- [**API Client Library**](./Kepware.Api/README.md): Simplify interaction with the Kepware Configuration API.
- [**Service for Synchronization**](./KepwareSync.Service/README.md):
  - Bi-directional synchronization between Kepware servers and local filesystems.
  - Support for one-way and two-way synchronization modes.
- [**Sample Applications**](./Kepware.Api.Sample/README.md): Demonstrates API usage with real-world examples.
- **HTTPS Support**: Certificate validation and secure connections.

## Projects
This repository contains the following projects:

### 1. `Kepware.Api`
A .NET library providing an easy-to-use client for interacting with the Kepware Configuration API. Includes functionality for managing channels, devices, tags, and more.

[Readme for Kepware.Api](./Kepware.Api/README.md)

**API reference documentation is available on [TBD]()**

### 2. `KepwareSync.Service`
A service application for synchronizing configurations between Kepware servers and the local filesystem. It supports monitoring and synchronization in real time.

[Readme for KepwareSync.Service](./KepwareSync.Service/README.md)

#### 1. Primary <-> Secondary Synchronization
Automatically synchronize configurations between two Kepware instances. Changes are detected via the REST Config API and propagated to the other instance.

```
+------------+       Sync        +------------+
|  Primary   |  <------------>   | Secondary  |
|  Kepware   |                   |  Kepware   |
+------------+                   +------------+
```

#### 2. GIT Versioning of Configurations
Synchronize configurations between a Kepware instance and the local filesystem bidirectionally. Changes in files are synced to Kepware and vice versa. Git operations like commits and pulls must be managed separately (e.g., using Git Sync Services or manual Git operations).

```
+------------+       Sync        +--------------+       Git        +-------------+
|  Kepware   |  <------------>  |  Local Files |  <------------>  |   GIT Repo  |
+------------+                   +--------------+                  +-------------+
```

#### 3. Mass Deployment of Centralized Configurations
Deploy a centralized GIT configuration across multiple Kepware instances. Configurations are provided locally via tools like Git or RSync and then synchronized to Kepware using the sync tool. Local specifics like device IP addresses or credentials can be customized using overwrite files.

```
           +--------------------+
           |   Central GIT Repo |
           +--------------------+
                   |
          (Git Sync / RSync)
                   |
+--------------+   +--------------+   +--------------+
| Kepware #1  |   | Kepware #2   |   | Kepware #n   |
| [Overwrite] |   | [Overwrite]  |   | [Overwrite]  |
+--------------+   +--------------+   +--------------+
```


### 3. `Kepware.Api.Sample`
Sample console applications demonstrating how to use `Kepware.Api` NuGet package to interact with the Kepware Configuration API. Includes examples for creating channels, devices, and tags.

[Readme for Kepware.Api.Sample](./Kepware.Api.Sample/README.md)

## Contribution Guidelines
We welcome contributions to this repository. Please review the [Repository Guidelines](./docs/repo-guidelines.md) for information on Commit Message Conventions, Pull Request process and other details.

## Licensing
This SDK and service application is provided "as is" under the MIT License. See the [LICENSE](./LICENSE.txt) file for details.

## Support
For any issues, please open an Issue within the repository. For questions or feature requests, please open a Discussion thread within the repository. 

See [Repository Guidelines](./docs/repo-guidelines.md) for more information.

## Need More Information

**Visit:**

- [Kepware.Api API Documentation on Github Pages]()
- [Kepware.com](https://www.kepware.com/)
- [PTC.com](https://www.ptc.com/)

---
Ready to dive in? Check out the project-specific Readmes for detailed information on how to get started!

