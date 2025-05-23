﻿# Deployment

FoxIDs support multiple deployment options and you can host it yourself both in the cloud or on-premises.

- Deploy using [Kubernetes](deployment-k8s.md) on-premises or in the cloud
- Deploy using [Docker](deployment-docker.md) on-premises or in the cloud - most for dev and test
- Deploy on [Windows Server with IIS](deployment-window-iis.md)
- Deploy in [Azure App Service Container](deployment-azure.md) 

or use [FoxIDs Cloud](https://www.foxids.com/action/signup).

> New [releases of FoxIDs](https://github.com/ITfoxtec/FoxIDs/releases) are continuously published to [Docker Hub](https://hub.docker.com/u/foxids), so make sure to update your installation at appropriate intervals.

After deployment consider to:

- Place your FoxIDs deployment securely behind a [reverse proxy](reverse-proxy.md).
- Improve the password quality by [uploading risk passwords](risk-passwords.md). 
- Configure [monitoring](monitoring.md).
- [Upload your users](users-upload.md) from a SVC file, with or without a password.

