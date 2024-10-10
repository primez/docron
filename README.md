# Docron

Docron is a scheduling tool designed to automate the start and stop of Docker containers based on user-defined schedules. With a simple interface, it streamlines container management.

## Reasoning

I created this application to provide an alternative for users who are not comfortable using `crontab` or those who prefer not to modify system files. Docron runs in its own container, with access only to its resources and other containers via the Docker API. I haven't found any existing solutions that offer this functionality, but I may have overlooked some.

## How to Use

### Docker Run

If you prefer a one-off command to start a container, use the following code:

```shell
docker run -d \
  --name docron \
  -p 8080:8080 \
  -e DockerConnection=unix:///var/run/docker.sock \
  -e DB='Data Source=/data/docron.db;' \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  -v /<yourDBFolder>/:/data \
  ghcr.io/primez/docron:latest
```

### Docker Compose
Alternatively, a Docker Compose file might look like this:
```shell
services:
  docron:
    image: ghcr.io/primez/docron:latest
    container_name: docron
    ports:
      - 8080:8080
    environment:
      - DockerConnection=unix:///var/run/docker.sock
      - DB=Data Source=/data/docron.db;
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - /<yourDBFolder>/:/data
```

### Parameters
`Docron` expects the following parameters to function properly:
- **ENV**: `DockerConnection` - This is the address that `Docron` will use to connect and gather information about containers for start/stop operations. If this parameter is not provided, it will default to a `Docker for Windows` environment.
- **ENV**: `DB` - `Docron` uses `SQLite` to store schedules. To store the database in a mounted volume, provide this parameter. Otherwise, the database will be created inside the container.
- **VOL**: `<yourDBFolder>` - If you want to persist the database between container updates, specify a host folder path to be mounted.

### Screenshots
Here are a few screenshots of the UI:
- ![1](https://github.com/user-attachments/assets/b51ff834-8f64-4ecb-bd0a-2ad0863807fb)
- ![2](https://github.com/user-attachments/assets/62e9ad64-03d9-454c-a453-c27ebac10c99)
- ![3](https://github.com/user-attachments/assets/b71591e6-42d1-42b4-9d71-f4a68e450e61)

