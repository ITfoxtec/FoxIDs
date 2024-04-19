# Docker

Clone or download ...
Navigate to the /Docker folder

Create voluems
docker volume create foxids-data

Create volumes on Windows host filesystem in C:\data\foxids-data and C:\data\foxids-data-config - Important: create folders before
docker volume create --driver local --opt type=none --opt device=C:\data\foxids-data --opt o=bind foxids-data


dev with http
docker-compose -f docker-compose-project.yaml -f docker-compose.development-http.yaml up -d
docker-compose -f docker-compose-image.yaml -f docker-compose.development-http.yaml up -d

dev with https
docker-compose -f docker-compose-project.yaml -f docker-compose.development-https.yaml up -d

prod
docker-compose -f docker-compose-image.yaml -f docker-compose.production.yaml up -d