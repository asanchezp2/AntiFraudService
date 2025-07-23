# Anti-Fraud Service

**Author:** Armando Sánchez Pérez  
**Email:** armandosanchezperez1986@gmail.com
**Copyright:** © 2025

This project implements an anti-fraud microservice to validate financial transactions in real-time. Each created transaction is evaluated by this service, which then updates its status.

## Problem Description

Every time a financial transaction is created, it must be validated by the anti-fraud microservice. After validation, the same service sends a message to update the transaction's status.

The possible transaction statuses are:
- `pending`: Initial state while validation is in progress.
- `approved`: The transaction has passed the anti-fraud checks.
- `rejected`: The transaction has failed the anti-fraud checks.

### Rejection Criteria

A transaction will be rejected if it meets any of the following conditions:
1. The transaction value is greater than **2000**.
2. The accumulated daily value for the source account exceeds **20000**.

## Tech Stack

- .NET 8
- ASP.NET Core Web API
- SQL Server
- Apache Kafka
- Docker & Docker Compose

## Prerequisites

Ensure you have the following installed on your system:
- Docker
- Docker Compose
- (Optional) Visual Studio 2022 or later with the "ASP.NET and web development" workload.

## How to Run the Project

You can run the entire solution using one of the following methods.

### Option 1: Using the Command Line

1. Clone this repository to your local machine.
2. Open a terminal and navigate to the project's root directory (where the `docker-compose.yml` file is located).
3. Run the following command to build and start all services in containers:

    ```bash
    docker-compose up -d --build
    ```

    This command will start:
    - `antifraud-api`: The API service (available at `http://localhost:5000`).
    - `sqlserver`: The SQL Server database.
    - `kafka` and `zookeeper`: The messaging broker for asynchronous communication.

4. To stop all services, run:
    ```bash
    docker-compose down
    ```

### Option 2: Using Visual Studio

1. Open the solution file (`.sln`) in Visual Studio.
2. Set `docker-compose` as the startup project.
3. Press `F5` or click the "Run" button. Visual Studio will handle building the images and starting the containers.

## API Endpoints

The API is available at `http://localhost:5000`.

### 1. Create a Transaction

Creates and submits a new transaction for validation. The transaction is initially created with a `pending` status.

- **URL**: `/api/Transaction`
- **Method**: `POST`
- **Body**:

  ```json
  {
    "sourceAccountId": "string (guid)",
    "targetAccountId": "string (guid)",
    "transferTypeId": 1,
    "value": 120.0
  }
  ```

- **Example with `curl`**:

  ```bash
  curl -X POST "http://localhost:5000/api/Transaction" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceAccountId": "d3a1e1a6-1234-5678-9101-a2b3c4d5e6f7",
    "targetAccountId": "e8b2f2b7-8765-4321-fedc-ba9876543210",
    "transferTypeId": 1,
    "value": 1500
  }'
  ```

### 2. Get a Transaction

Retrieves the details and current status of a transaction by its ID.

- **URL**: `/api/Transaction/{id}`
- **Method**: `GET`
- **Example with `curl`**:

  ```bash
  # Replace {id} with the ID of the transaction you want to query
  curl -X GET "http://localhost:5000/api/Transaction/{id}"
  ```

## Architecture Overview

The anti-fraud service follows a microservices architecture pattern with the following components:

- **API Layer**: Exposes REST endpoints for transaction management
- **Business Logic**: Implements fraud detection rules and validation logic
- **Data Layer**: Persists transaction data in SQL Server
- **Messaging**: Uses Apache Kafka for asynchronous communication and status updates

## Development Notes

- The service automatically validates transactions against the defined fraud criteria
- Transaction status updates are handled asynchronously via Kafka messaging
- Daily accumulation calculations are performed per source account
- All services are containerized for easy deployment and scalability