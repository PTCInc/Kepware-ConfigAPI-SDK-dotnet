# Kepware Configuration API SDK for .NET

[![Build Status](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions)
[![Build Status](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions/workflows/docker-build-and-push.yml/badge.svg)](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions)

## Overview
The Kepware Configuration API SDK for .NET provides tools and libraries to interact with the Kepware REST API, enabling configuration management for Kepware servers. This repository includes examples and utilities to streamline development, including a service for continuous synchronization and an API client library.

## Features
- **API Client Library**: Simplify interaction with the Kepware Configuration API.
- **Service for Synchronization**:
  - Bi-directional synchronization between Kepware servers and local filesystems.
  - Support for one-way and two-way synchronization modes.
- **Sample Application**: Demonstrates API usage with real-world examples.
- **HTTPS Support**: Certificate validation and secure connections.

## Projects
This repository contains the following projects:

### 1. `KepwareSync.Service`
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

### 2. `Kepware.Api`
A .NET library providing an easy-to-use client for interacting with the Kepware Configuration API. Includes functionality for managing channels, devices, tags, and more.

[Readme for Kepware.Api](./Kepware.Api/README.md)

### 3. `Kepware.Api.Sample`
A sample console application demonstrating how to use `Kepware.Api` to interact with the Kepware Configuration API. Includes examples for creating channels, devices, and tags.

[Readme for Kepware.Api.Sample](./Kepware.Api.Sample/README.md)


## Contribution Guidelines
We welcome contributions to this repository. Please follow these guidelines:

### Commit Message Convention
We adhere to the [Conventional Commits](https://www.conventionalcommits.org/) specification:

**Format:** `<type>(<scope>): <short summary>`

**Examples:**
- `feat(api): add support for new configuration objects`
- `fix(sync): resolve issue with file monitoring`
- `docs(service): update usage instructions`

**Types:**
- `feat`: New features.
- `fix`: Bug fixes.
- `docs`: Documentation updates.
- `style`: Code style changes.
- `refactor`: Code restructuring without functional changes.
- `test`: Adding or updating tests.
- `chore`: Maintenance tasks, e.g., dependency updates.

## Licensing
This SDK is provided "as is" under the MIT License. See the [LICENSE](./LICENSE.txt) file for details.

## Support
For issues or feature requests, please open a ticket in the GitHub repository.

---
Ready to dive in? Check out the project-specific Readmes for detailed information on how to get started!

