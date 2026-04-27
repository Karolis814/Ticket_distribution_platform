# Ticket Distribution Platform

## Project Description

This is a team project aimed at developing a ticket distribution platform that allows users to browse events, purchase tickets, and manage content.

The system is designed as a fully functional web application with a strong focus on both functional and non-functional (quality) requirements.

---

## Functional Requirements (Overview)

The system allows users to:

* Register, log in, and log out
* Browse and search events
* Filter events by date, location, or category
* View ticket details before purchase
* Purchase tickets for selected events
* Access purchased tickets
* Download tickets and receive them via email
* View QR codes linked to ticket information

---

## Non-Functional Requirements

The system is designed with the following quality attributes:

* **Concurrency** – multi-tab usage within the same session
* **Security** – protection against SQL injection
* **Data access** – efficient database interaction
* **Data consistency** – optimistic locking
* **Memory management** – efficient resource usage
* **Async / non-blocking** – responsive system behavior
* **Logging** – tracking user and system actions
* **Extensibility** – support for easy system extension

---

## Technologies

* .NET 9
* ASP.NET Core Web API
* Entity Framework Core
* Dependency Injection
* SignalR (optional, for real-time features)

---

## Project Structure

The system follows a modular architecture:

* API layer
* Business logic layer
* Data access layer

---
