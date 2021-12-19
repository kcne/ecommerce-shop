# Configuration
### This file serves as a guide to configure and install project locally - establish a test enviroment so the tests on the API side can be performed.

# Prerequisites: 
## 1. .NET 5
### <a href="https://dotnet.microsoft.com/en-us/download/dotnet/5.0" target="_blank">Download</a> and install .NET 5 SDK and Runtime. 
## 2. Docker
### Setup <a href="https://www.docker.com/products/docker-desktop" target="_blank">Docker</a>.

# Setup

## 1. Docker Image
 ### Now that we have everything we need. Start docker, open the folder from the terminal and run the following command:
 ```
docker-compose up --detach
 ```
 ### After running the command open docker. 
 ### There should e-commerce container - make sure it's running.

## 2. IDE 
### Open the project from the desired IDE. 
### To build the project first we go to API directory, build and run:
```
cd API
dotnet build
dotnet run
```
### You should now be able to see the website from https://localhost:5001 running locally.
## If everything is running correctly the test enviroment is ready.


