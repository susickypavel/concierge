# Bot

## 1. Local development

### 1.1 Prerequisites

- [.NET Core 7](https://dotnet.microsoft.com/en-us/download)
- [Docker](https://www.docker.com)
- [Jetbrains Rider](https://www.jetbrains.com/rider/) - Optional but recommended

### 1.2 Setup

1. **Configure environment variables. See 2.2**
2. Start services from [docker-compose.yml](./docker-compose.yml)
3. Start the project using IDE or CLI

## 2. Environment variables

### 2.1 Summary

| Name          | Purpose               |
|---------------|-----------------------|
| DISCORD_TOKEN | Authenticates the bot |

### 2.2 Configure the environment variable through CLI

```sh
dotnet user-secrets set DISCORD_TOKEN "<TOKEN_VALUE>"
```
