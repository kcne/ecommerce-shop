# Online Web Store

This repository contains the source code for an online web store application built with .NET 5.0 as the backend framework and Angular 11 as the frontend framework. This application allows users to browse products, add them to their cart, and make purchases online.

Note: All changes in building process are documented in `CHANGES.md` file. You can open file for more details on implementation.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

Before you can run the project, you will need to install the following software:

- [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [Node.js](https://nodejs.org/) (which includes npm for Angular)
- [Angular CLI](https://angular.io/cli) (version 11)

1. **Clone the repository**

   ```bash
   git clone https://github.com/your-username/your-repository-name.git
   cd your-repository-name
   ```

2. ** Install backend dependencies**
    ```bash
    dotnet restore
    ```

3. **Install frontend dependencies**
    ```bash
    cd client
    npm install
    ```

4. **Start the backend server.**
    ```bash
    cd ..
    dotnet run
    ```

5. **Start the Angular application in separate terminal session.**
    ```bash
    cd client
    ng serve
    ```

## Features
- Authorization and authentication
- Product browsing
- Shopping cart functionalities
- Order checkout and payment processing (demo)
- Dockerized for easy hosting


