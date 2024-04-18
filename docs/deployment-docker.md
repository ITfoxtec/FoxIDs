
Create voluems
docker volume create foxids-data

Create volumes on Windows host filesystem in C:\data\foxids-data and C:\data\foxids-data-config - Important: create folders before
docker volume create --driver local --opt type=none --opt device=C:\data\foxids-data --opt o=bind foxids-data


dev with http
docker-compose -f docker-compose-project.yml -f docker-compose.development-http.yml up -d
docker-compose -f docker-compose-image.yml -f docker-compose.development-http.yml up -d

dev with https
docker-compose -f docker-compose-project.yml -f docker-compose.development-https.yml up -d

prod
docker-compose -f docker-compose-image.yml -f docker-compose.production.yml up -d