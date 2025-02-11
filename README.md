# FiwareClient
A C# client library to connect to an Orion Context Broker using the FIWARE protocol.

# Test Environment
To test the client with an Orion Context Broker, the easiest method is to compile and use a Docker image containing the Orion Context Broker as well as a pre-defined MongoDB. For this to work, download and install [Docker Desktop](https://hub.docker.com/search?q=&type=edition&offering=community&platform=desktop) which comes with [Docker Compose](https://docs.docker.com/compose/). The following Docker Compose script will create such a container.
```yaml
version: "3"

services:
  mongo:
    image: mongo:3.2
    command: --nojournal
  orion:
    image: fiware/orion
    links:
      - mongo
    ports:
      - "1026:1026"
    command: -dbhost mongo
```
Save this script as `docker-compose.yml`, navigate to the directory and run `docker-compose up`. This will download the required images, compose a new Docker container and start it.

After this, the Orion Context Broker should be available under `http://localhost:1026`. For further information refer to the [FIWARE API documentation](https://fiware-orion.readthedocs.io/en/master/user/walkthrough_apiv2/index.html).

