# StudentRoutineTrackerApi

A robust, secure, and extensible ASP.NET Core Web API for managing student routines, notes, and activities. This project provides endpoints for user authentication, note management, and activity tracking, following modern best practices and SOLID principles.

---

## Features

- **User Authentication:** Register, login, and JWT-based authentication.
- **Notes Management:** Create, read, update, and delete personal notes with tags and media attachments.
- **Activity Tracking:** Track note activity and streaks.
- **Role-based Authorization:** Secure endpoints with granular access control.
- **Comprehensive Logging:** Structured logging for all critical actions.
- **Extensible Architecture:** Clean separation of concerns using repositories, services, and DTOs.

---

## Technologies Used

- **.NET 8 / C# 12**
- **ASP.NET Core Web API**
- **MongoDB** (via MongoDB.Driver)
- **JWT Authentication**
- **Serilog** for logging
- **Swagger/OpenAPI** for API documentation

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MongoDB](https://www.mongodb.com/try/download/community)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)

### Installation

1. **Clone the repository:** git clone https://github.com/piyushsingh022002/RecoTrackAPI.git cd RecoTrackAPI
2. **Configure the application:**
   - Update `appsettings.json` with your MongoDB connection string and JWT settings.

3. **Restore dependencies:**dotnet restore
   
4. **Run the application:** dotnet run


5. **Access the API documentation:**
- Navigate to `https://localhost:<port>/swagger` in your browser.

---

## Usage

### Authentication

- **Register:** `POST /api/auth/register`
- **Login:** `POST /api/auth/login`
- **Get Current User:** `GET /api/auth/user` (JWT required)

### Notes

- **Get All Notes:** `GET /api/notes`
- **Get Note by ID:** `GET /api/notes/{id}`
- **Create Note:** `POST /api/notes`
- **Update Note:** `PUT /api/notes/{id}`
- **Delete Note:** `DELETE /api/notes/{id}`

> All notes endpoints require authentication via JWT.

---

## Project Structure
Controllers/        
// API controllers Models/    
// Domain and DTO models Repositories/    
// Data access logic Services/       
// Business logic Program.cs        
// Application entry point appsettings.json  
// Configuration


---

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

---

## License

This project is licensed under the MIT License.

---

## Contact

For questions or support, please open an issue or contact [Piyush Singh](mailto:piyushsingh022002@gmail.com).

