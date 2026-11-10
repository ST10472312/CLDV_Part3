# CoffeeNChill – Deployment Guide

## 1. Deployment Overview
CoffeeNChill is deployed using Docker containers on an Ubuntu Azure Virtual Machine.

## 2. Deployment Environment
- Cloud provider: Microsoft Azure
- Operating system: Ubuntu 24.04
- Container platform: Docker and Docker Compose
- Application access: HTTP port 5000
- API documentation: `/swagger`
- Health endpoint: `/health`

## 3. Application Containers
- CoffeeNChill Gateway
- CoffeeNChill Functions
- Azurite Storage Emulator

## 4. Deployment Configuration
Production configuration is managed using `docker-compose.prod.yml` and a private `.env` file on the VPS. Never commit `.env` or passwords, tokens, or private keys to GitHub.

## 5. Verification
The deployment was verified using:
- Public Swagger endpoint
- Public health endpoint
- Postman menu smoke test – HTTP 200
- Get all menu items – HTTP 200

## 6. Security
- SSH access is restricted to the configured SSH connection.
- Functions and Azurite ports are not publicly exposed.
- Production configuration uses environment variables for sensitive values.

## 7. Cost Management
Stop and deallocate the Azure VM when deployment testing is complete. Storage and networking resources may still incur charges.