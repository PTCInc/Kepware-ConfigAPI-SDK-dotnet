# KepwareSync.Service (Currently in Beta)

[![Build Status](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions)
[![Build Status](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions/workflows/docker-build-and-push.yml/badge.svg)](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions)

## Overview
`KepwareSync.Service` is a CLI and service tool designed to synchronize configuration data between Kepware servers and the local filesystem. It supports both one-way and two-way synchronization, making it ideal for real-time configuration management.

## Use Cases

### 1. Primary <-> Secondary Synchronization
Automatically synchronize configurations between two Kepware instances. Changes are detected via the REST Config API and propagated to the other instance.

```
+------------+       Sync        +------------+
|  Primary   |  <------------>   | Secondary  |
|  Kepware   |                   |  Kepware   |
+------------+                   +------------+
```

### 2. GIT Versioning of Configurations
Synchronize configurations between a Kepware instance and the local filesystem bidirectionally. Changes in files are synced to Kepware and vice versa. Git operations like commits and pulls must be managed separately (e.g., using Git Sync Services or manual Git operations).

```
+------------+       Sync        +--------------+       Git        +-------------+
|  Kepware   |  <------------>  |  Local Files |  <------------>  |   GIT Repo  |
+------------+                   +--------------+                  +-------------+
```

### 3. Mass Deployment of Centralized Configurations
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

## Features
- **Command-Line Interface**: Execute synchronization tasks directly from the command line.
- **Service Mode**: Continuous monitoring and synchronization between Kepware servers and the local filesystem.
- **Support for Multiple Servers**: Synchronize with both primary and secondary Kepware servers.
- **Flexible Configuration**: Easily configurable using `appsettings.json`, environment variables, or CLI parameters.
- **HTTPS Support**: Secure connections with optional certificate validation.
- **Overwrite Configuration Support**: Customize configurations dynamically using YAML files with environment variable placeholders.

## Usage

### CLI Commands
#### General Syntax
```bash
Kepware.SyncService [command] [options]
```

#### Commands
- **(default)**: Run as a background agent, monitoring for changes and starting synchronization when detected.
- **SyncToDisk**: Synchronize configuration data from Kepware servers to the local filesystem.
- **SyncFromDisk**: Apply configuration data from the local filesystem to Kepware servers.

#### Global Options
| Option                                     | Description                                                                                              | Default Value     |
|--------------------------------------------|----------------------------------------------------------------------------------------------------------|-------------------|
| `--primary-kep-api-username`               | Primary Kepware API username (**required**).                                                             |                   |
| `--primary-kep-api-password`               | Primary Kepware API password (**required** if no password file is provided).                             |                   |
| `--primary-kep-api-password-file`          | Path to a file containing the primary Kepware API password.                                              |                   |
| `--primary-kep-api-host`                   | Primary Kepware API host URL (**required**).                                                             |                   |
| `--directory`                              | Directory for storing configuration files (**required**).                                                |                   |
| `--http-timeout`                           | HTTP timeout in seconds.                                                                                 | `60`              |
| `--http-disable-cert-check`                | Disable certificate validation.                                                                          | `false`           |
| `--persist-default-value`                  | Persist default values during synchronization.                                                           | `false`           |
| `--kep-sync-direction`                     | Synchronization direction (`DiskToKepware`, `KepwareToDisk`, `KepwareToDiskAndSecondary`, `KepwareToKepware`). |                   |
| `--kep-sync-mode`                          | Synchronization mode (`OneWay` or `TwoWay`).                                                             |                   |
| `--kep-sync-throttling`                    | Throttling time in milliseconds after detecting a change before synchronization starts.                  |                   |
| `--version`                                | Show version information.                                                                                |                   |
| `-?, -h, --help`                           | Show help and usage information.                                                                         |                   |

#### Additional Options for Sync Commands
| Option                                     | Description                                                                                              |
|--------------------------------------------|----------------------------------------------------------------------------------------------------------|
| `--primary-overwrite-file`                 | Path to a YAML file with overwrite configurations for the primary Kepware server.                         |
| `--secondary-kep-api`                      | List of secondary Kepware API configurations in the format `username:password@host`.                      |
| `--secondary-password-files`               | List of paths to files containing secondary Kepware API passwords.                                       |
| `--secondary-overwrite-files`              | List of paths to YAML files with overwrite configurations for secondary Kepware servers.                  |

### CLI Examples
1. **Monitor and Bidirectional Synchronization (Kepware <-> Disk)**
   ```bash
   Kepware.SyncService --primary-kep-api-username Administrator \
      --primary-kep-api-password StrongAdminPassword2025! \
      --primary-kep-api-host https://localhost:57512 \
      --directory ./ExportedYaml
   ```

2. **Synchronize Kepware to Disk:**
   ```bash
   Kepware.SyncService SyncToDisk --primary-kep-api-username Administrator \
      --primary-kep-api-password-file ./secrets/password.txt \
      --primary-kep-api-host https://localhost:57512 \
      --directory ./ExportedYaml
   ```

3. **Synchronize Disk to Kepware with Overwrite Config:**
   ```bash
   Kepware.SyncService SyncFromDisk --primary-kep-api-username Administrator \
      --primary-kep-api-password StrongAdminPassword2025! \
      --primary-kep-api-host https://localhost:57512 \
      --primary-overwrite-file ./overrides/device_config.yaml \
      --directory ./ExportedYaml
   ```

### Overwrite Configuration Example (`device_config.yaml`)
```yaml
Channels:
  - Name: Channel1
    Overwrite:
      servermain.MULTIPLE_TYPES_DEVICE_DRIVER: "ModbusTCP"
    Devices:
      - Name: Device1
        Overwrite:
          servermain.DEVICE_ID_STRING: "<${MODBUS_DEVICE_IP}>.0"
          servermain.MULTIPLE_TYPES_DEVICE_DRIVER: "ModbusTCP"
        Tags:
          - Tag1:
              Address: 471
              DataType: Word
              ClientAccess: R/W
          - Tag2:
              Address: 472
              DataType: Word
              ClientAccess: R/W
```
*Environment variables like `${MODBUS_DEVICE_IP}` will be resolved during synchronization.*


### Environment Variable Configuration
The service also supports environment variables for configuration using `AddEnvironmentVariables`. Example environment variables:

| Environment Variable               | Example Value               |
|------------------------------------|-----------------------------|
| `KEPWARE__PRIMARY__USERNAME`       | Administrator               |
| `KEPWARE__PRIMARY__PASSWORD`       | StrongAdminPassword2025!         |
| `KEPWARE__PRIMARY__HOST`           | https://localhost:57512     |
| `KEPWARE__DISABLECERTIFICATEVALIDATION` | true                  |
| `STORAGE__DIRECTORY`               | ExportedYaml                |

Set these variables in your system or Docker container to override appsettings.json.

### Sample Configuration File (`appsettings.json`)
The `appsettings.json` file provides an alternative way to configure `KepwareSync.Service`. You can define primary and secondary server configurations, timeouts, and storage settings.

```json
{
  "Kepware": {
    "DisableCertificateValidation": false,
    "TimeoutInSeconds": 60,
    "Primary": {
      //"Username": "Administrator",
      //"Password": "<Password>",
      //"PasswordFile": "<Path to Password File>",
      //"Host": "https://localhost:57512",
      //"OverwriteConfigFile": "<Path to YAML overwrite file>"
    },
    "Secondary": [
      //{
      //  "Username": "Administrator",
      //  "Password": "<Password>",
      //  "PasswordFile": "<Path to Password File>",
      //  "Host": "https://localhost:57513",
      //  "OverwriteConfigFile": "<Path to YAML overwrite file>"
      //}
    ]
  },
  "Storage": {
    "Directory": "ExportedYaml"
  }
}
```

*Commented lines indicate optional configurations that can be enabled as needed. Overwrite files allow dynamic adjustments, such as IP changes, during synchronization.*


### Docker Deployment

To run `KepwareSync.Service` in a Docker container, use the following instructions. Ensure that you have Docker installed and running on your system.

#### Run the Container with Volume Mounts
If you want to access the persisted yaml files, you can mount the `STORAGE__DIRECTORY` as shown.

```bash
docker run -d \
  --name kepware-sync-service \
  --restart always \
  -v ./config:/app/config \
  -e STORAGE__DIRECTORY=/app/config \
  -e KEPWARE__PRIMARY__USERNAME=Administrator \
  -e KEPWARE__PRIMARY__PASSWORD=StrongAdminPassword2025! \
  -e KEPWARE__PRIMARY__HOST=https://localhost:57512 \
  kepware-sync-service:latest
```

This command:
- Runs the container in detached mode (`-d`).
- Assigns a name to the container (`--name kepware-sync-service`).
- Restarts the container automatically on failure or system reboot (`--restart always`).
- Mounts the local `config` directory to `/app/config` inside the container (`-v $(pwd)/config:/app/config`).
- Uses environment variables to configure the primary Kepware connection.

#### Docker Compose Setup

If you want to automate Kepware deployment with a Git-based configuration, you can use Git-Sync as a sidecar alongside *KepwareSync.Service*. This setup ensures that configuration changes in your Git repository are automatically synchronized with Kepware, enabling a scalable and GitOps-driven deployment

For a more structured deployment, use `docker-compose.yml`:

```yaml
version: '3.8'

services:
  kepware:
    image: kepedge:<version>
    pull_policy: always
    networks:
      - ke_network
    ports:
      - "57513:57513"
      - "49330:49330"
    environment:
      - EDGEADMINPW=StrongAdminPassword2025!
    volumes:
      - ./data/config:/opt/tkedge/v1/.config
    restart: unless-stopped

  kepware-sync:
    image: kepware-sync-service:latest
    pull_policy: always
    depends_on:
      - kepware
    networks:
      - ke_network
    volumes:
      - config_volume:/app/config
    environment:
      - KEPWARE__PRIMARY__USERNAME=Administrator
      - KEPWARE__PRIMARY__PASSWORD=StrongAdminPassword2025!
      - KEPWARE__PRIMARY__HOST=https://kepware:57513
      - STORAGE__DIRECTORY=/app/config
    restart: unless-stopped

  git-sync:
    image: k8s.gcr.io/git-sync:v3.4.1
    pull_policy: always
    depends_on:
      - kepware-sync
    networks:
      - ke_network
    environment:
      - GIT_SYNC_REPO=https://your-repo.git
      - GIT_SYNC_BRANCH=main
      - GIT_SYNC_WAIT=10
    volumes:
      - config_volume:/repo
    restart: unless-stopped

volumes:
  config_volume:

networks:
  ke_network:
    driver: bridge
```

## Recommendations
- Always back up your Kepware server configuration before performing any synchronization.
- For two-way synchronization, ensure there are no conflicting changes on both sides.
- Use `--persist-default-value` to retain default values in configurations if required.

## Licensing
This tool is provided "as is" under the MIT License. See the [LICENSE](../LICENSE.txt) file for details.

## Support
For any issues, please open an Issue within the repository. For questions or feature requests, please open a Discussion thread within the repository.

See [Repository Guidelines](../docs/repo-guidelines.md) for more information.
