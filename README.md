# TicketSystem0.3-master

## Chapter One: Introduction

The Traffic Violation Ticket System is a web-based application built with ASP.NET Core MVC to manage traffic violation cases in a structured and secure way. It supports the main users involved in the workflow, including officers, office staff, administrators, and drivers or account holders, while keeping ticket records organized in a central database.

The system is designed to reduce manual processing, improve access to violation details, and make ticket handling easier across different roles. It includes role-based authentication, ticket lookup and detail views, QR-based ticket access, plate recognition integration, and support pages for help, manuals, traffic laws, and contact information.

### Purpose

This project aims to provide a practical digital platform for recording, reviewing, and managing traffic violation tickets. It helps staff process cases faster and allows users to view their own ticket information in a controlled and secure manner.

### Scope

The application covers user login, ticket management, feedback submission, violation records, reporting pages, and support tools for end users. It also includes integration points for license plate recognition and QR code-based ticket access.

### Main Technologies

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Cookie-based authentication
- QR code generation
- Plate recognition service integration

### Project Summary

Overall, this system provides a centralized and role-aware solution for traffic violation ticket management. It is intended to improve efficiency, transparency, and accessibility for both administrative users and the public.

## Chapter Two: Analyst Chapter

This chapter presents the analysis view of the Traffic Violation Ticket System. It explains who studies the system, who interacts with it, and how the main behaviors and data structures are organized.

### 2.1 System Analyst

The system analyst is responsible for studying the business needs of the traffic violation process and translating them into software requirements. In this project, the analyst identifies how tickets are created, reviewed, paid, and tracked across the different user roles. The analyst also ensures that the system supports secure login, role-based access, reporting, and ticket verification.

### 2.2 Actors Analysis

The system includes several main actors:

- Admin: manages users, violations, feedback, and overall system settings.
- Officer: creates and reviews violation tickets, and follows up on ticket details.
- Office Staff: processes tickets, scans plates or QR codes, and handles administrative work.
- User: views personal tickets, receives receipts, and pays violations.
- External Services: plate recognition and QR code services used to support ticket processing and access.

Each actor has a specific role in the workflow, and the system limits access according to the actor's permissions.

### 2.3 Use Case Diagram

The use case diagram describes the main interactions between the actors and the system. It shows how users log in, view ticket details, submit feedback, process violations, scan plates, and complete payment actions. It also shows that administrators and office staff have additional management functions beyond the public user role.

The project repository includes the use case diagram as [Documents/Use-Case.pdf](Documents/Use-Case.pdf).

### 2.4 Class Diagram

The class diagram represents the main data and logic structures of the application. The core classes include User, Ticket, Violation, Feedback, and the view models used for login, dashboard, reports, and reset password operations. These classes define how records are stored, validated, and passed between controllers and views.

Separate class diagram image files are not present in the repository, so this section summarizes the class structure based on the actual code.

### 2.5 Activity Diagram

The activity diagram explains the flow of work inside the system. A typical flow starts when a user logs in, continues to role-based navigation, and then moves to actions such as creating a ticket, scanning a plate, checking a ticket by QR code, or paying a fine. If the action is successful, the system updates the relevant record and displays the result; if not, it shows an error or access-denied message.

Separate activity diagram image files are not present in the repository, so this section describes the process flow textually based on the implemented controllers and views.

### 2.6 Analyst Summary

The analyst chapter shows that the project is built around clear roles, controlled access, and a structured workflow. This analysis supports the design of the system and helps explain how the application satisfies the needs of traffic violation management.

## Chapter Three: Software Requirements

This chapter describes the software requirements needed to run and support the Traffic Violation Ticket System. It includes both the technical environment required to host the application and the core functional requirements expected from the software.

### 3.1 Software Environment Requirements

The system is built as an ASP.NET Core MVC application and requires the following software components:

- .NET 9 SDK or compatible runtime
- ASP.NET Core MVC framework
- SQL Server database engine
- Entity Framework Core for data access
- A modern web browser such as Microsoft Edge, Google Chrome, or Firefox
- Visual Studio or Visual Studio Code for development and maintenance

### 3.2 Functional Requirements

The system should provide the following functional capabilities:

- User registration and authentication
- Role-based login for Admin, Officer, Office Staff, and User accounts
- Secure logout and access control
- Ticket creation, viewing, and update operations
- Ticket lookup through QR code and ticket ID
- Viewing personal ticket details and receipts
- Payment processing workflow for fines
- Plate recognition support for traffic enforcement
- Feedback submission and review
- Reporting and dashboard pages for administrative users
- Support pages such as help, manual, traffic laws, and contact information

### 3.3 Non-Functional Requirements

The application should also satisfy the following non-functional requirements:

- Security: protect user data with cookie-based authentication and role checks
- Reliability: preserve ticket records accurately in the database
- Usability: provide a clear and responsive interface for different user roles
- Maintainability: keep controllers, views, and services organized for future updates
- Performance: load ticket and dashboard pages in a reasonable time for normal usage
- Compatibility: work correctly on common desktop browsers

### 3.4 Data Requirements

The system stores and uses data for users, tickets, violations, feedback, and reports. The database must support:

- User identity and role information
- Ticket numbers, violation details, fine amounts, and payment status
- Feedback messages and timestamps
- Violation definitions and descriptions

### 3.5 Operational Requirements

For proper operation, the system requires:

- A configured SQL Server connection string
- Valid settings for plate recognition and QR code services
- Correct authentication and authorization settings
- Access to the web root for uploaded tutorial and support files

For LPR in production, set one of these values on the hosting environment:

- `PlateRecognizer:ApiToken`
- `PLATE_RECOGNIZER_API_TOKEN`
- `PlateRecognizer__ApiToken`

On MonsterASP, add the token in the site's application settings or environment variables, then redeploy the app so the LPR client can authenticate with the Plate Recognizer API.

### 3.6 Chapter Summary

The software requirements define the technical foundation and expected behavior of the system. They ensure that the application can be deployed, used, and maintained in a secure and organized way.

## Chapter Four: Database Design

This chapter describes the database structure used by the Traffic Violation Ticket System. The application uses Entity Framework Core with SQL Server to store users, tickets, violations, and feedback in separate tables with clear relationships.

### 4.1 Database Design Overview

The database is designed to keep the main entities of the system organized and easy to manage. Each table represents one important part of the application workflow, and the relationships link tickets to the users who own them.

The main database entities are:

- Users
- Tickets
- Violations
- Feedbacks

### 4.2 Tables and Fields

#### Users Table

This table stores account and identity information for all system users.

Key fields include:

- Email: primary key
- Password
- Name
- Birth_Date
- Sex
- Blood_Type
- Address
- License_Status
- Digree
- Role
- FailedLoginAttempts
- LockoutEnd

#### Tickets Table

This table stores traffic violation ticket records.

Key fields include:

- Ticket_Id: primary key
- Violations
- Plate_Number
- Violation_Place
- IssuedBy
- Notes
- Status
- Ticket_Time
- Due_Date
- Appealed
- FineAmount
- VoiceReportPath
- Email: foreign key to Users

#### Violations Table

This table stores the list of violation types that can be assigned in the system.

Key fields include:

- Id: primary key
- Name
- Cost
- IsActive

#### Feedbacks Table

This table stores messages sent by users through the feedback feature.

Key fields include:

- Id: primary key
- Name
- Message
- CreatedAt
- IsRead

### 4.3 Relationships

The database currently includes the following relationship:

- One User can have many Tickets.
- Each Ticket belongs to one User through the Email foreign key.

This relationship supports role-based ticket access and allows each user to view only their own records.

### 4.4 ERD Diagram

The ERD diagram shows the relationship between the main database entities and how they interact with each other. In this project, the ERD is represented by the logical entity diagram stored in the Documents folder.

Suggested diagram reference:

- [Logical ERD - core entities](Documents/Logical%20ERD%20%E2%80%94%20core%20entities.png)

The ERD helps explain how the Users, Tickets, Violations, and Feedbacks tables are structured in the system.

### 4.5 Database Summary

The database design provides a simple and practical structure for storing all core data needed by the system. It supports secure access, organized ticket tracking, and clear data separation for future maintenance and expansion.

## Chapter Five: Implementation and UI Design

This chapter explains how the Traffic Violation Ticket System was implemented and how the user interface was designed. It also summarizes the software framework used to build the application.

### 5.1 Framework

The project is implemented using the following framework and supporting technologies:

- ASP.NET Core MVC as the main web framework
- Entity Framework Core for database access and model mapping
- SQL Server as the relational database
- Cookie-based authentication for login and session management
- QR code generation for ticket access and receipt support
- Plate recognizer service integration for vehicle plate processing

This framework combination provides a structured MVC architecture, secure authentication, and flexible database-driven behavior.

### 5.2 Implementation Overview

The application is divided into controllers, models, services, views, and data access layers.

- Controllers handle requests such as login, ticket operations, user access, office workflows, and admin functions.
- Models represent the core business entities, including users, tickets, violations, and feedback.
- Services handle reusable features such as QR code generation and license plate recognition.
- The database context manages Entity Framework communication with SQL Server.
- Views present the interface for each role and action in the system.

The implementation follows the MVC pattern, which keeps the business logic, UI, and data access separated and easier to maintain.

### 5.3 User Interface Design

The UI was designed to be responsive, role-based, and easy to use in both desktop and mobile browsers.

The main UI design features are:

- Right-to-left Arabic layout support for local usability
- Responsive pages built with Bootstrap and custom CSS
- Tajawal font for a clean Arabic-friendly appearance
- Separate layouts for different roles such as user, office staff, and admin
- Sidebar and navigation menus that adapt to mobile screens
- Visual icons and color accents for readability and faster navigation

The interface also uses different presentation styles depending on the role. The user layout uses Bootstrap 5 RTL, the admin layout uses a Tailwind-based dashboard style, and the office layout uses a custom responsive navigation design.

### 5.4 Main Implemented Modules

The system implementation includes the following modules:

- Authentication module for login and logout
- Ticket module for viewing, processing, and paying violations
- Admin module for user, violation, and feedback management
- Office module for registration, ticket handling, and report workflows
- User module for personal ticket viewing and receipts
- Support module for manuals, help pages, traffic laws, and contact information

Each module is connected to the relevant controller and view pages in the project.

### 5.5 Implementation Notes

The system also includes important implementation details such as role checking, ticket ownership validation, lockout protection after repeated failed login attempts, and support for QR-based ticket access. These behaviors help protect the system and keep ticket records accurate.

### 5.6 Chapter Summary

The implementation chapter shows that the project uses a clear MVC structure and a responsive UI design built around the needs of each user role. The selected framework and interface approach make the system practical, secure, and easy to extend.

## Chapter Six: Testing, Results, and Conclusion

This final chapter summarizes the testing approach, the expected results of the system, and the overall conclusion of the project.

### 6.1 Testing Overview

The Traffic Violation Ticket System was tested by checking the main application flows and validating that the pages and controllers work correctly with the database and user roles. The testing focused on the following areas:

- Login and logout behavior
- Role-based access control
- Ticket creation and ticket viewing
- QR-based ticket opening
- Receipt generation and print view
- Feedback submission
- Plate recognition and payment workflow support
- Responsive UI behavior on different screen sizes

### 6.2 Test Results

The system behavior supports the following expected results:

- Users can sign in according to their assigned roles.
- Unauthorized users are blocked from restricted pages.
- Ticket records are linked to the correct user account.
- Office and admin pages provide role-specific management functions.
- The user interface remains readable and usable on desktop and mobile devices.
- Database records are stored and retrieved through Entity Framework Core.

These results show that the system meets the main requirements described in the earlier chapters.

### 6.3 Project Outcome

The final outcome of the project is a web-based traffic violation ticket management system that organizes user accounts, ticket records, violation information, and support tools in one application. The system improves access to ticket data, supports secure role-based workflows, and reduces manual handling of violations.

### 6.4 Conclusion

The Traffic Violation Ticket System successfully demonstrates how ASP.NET Core MVC can be used to build a secure and practical administrative application. The project combines database design, role management, QR access, payment support, and a responsive user interface to create a complete traffic violation management solution.

### 6.5 Final Summary

This project provides a useful foundation for future improvements such as advanced reporting, deeper payment integration, additional analytics, and enhanced automation for traffic enforcement operations.

## Chapter Seven: Conclusion

The Traffic Violation Ticket System was developed as a complete web-based solution for managing traffic violation records, user accounts, ticket workflows, and support services. The project combines secure authentication, role-based access, database-driven ticket management, QR-based ticket viewing, and a responsive interface designed for different user roles.

### 7.1 Results

The final system achieved the main expected results of the project:

- Users can log in according to their assigned roles.
- Tickets are stored, retrieved, and displayed from the database correctly.
- QR-based ticket access works for viewing ticket details.
- Admin and office users have access to management pages suited to their responsibilities.
- The interface remains usable on both desktop and mobile screens.

These results show that the system meets the core goals defined in the earlier chapters.

### 7.2 Challenges

Several challenges were encountered during the development of the system:

- Managing role-based access across multiple controllers and layouts.
- Keeping ticket ownership secure so users can only open their own records.
- Organizing the user interface for three different role groups without making the design inconsistent.
- Handling integration points for QR code generation and plate recognition services.
- Structuring the database so it remains simple while still supporting all required features.

These challenges were addressed by separating the application into clear layers and applying validation and access checks where needed.

### 7.3 Future Development

The project can be expanded in the future with additional features such as:

- More detailed reports and analytics dashboards.
- Stronger payment gateway integration.
- Automated notifications for ticket status and due dates.
- Improved activity logging and audit trails.
- Better support for mobile-focused workflows and field operations.
- More advanced plate recognition and violation detection features.

These improvements would make the system more efficient, more informative, and more suitable for larger-scale deployment.

### 7.4 Final Conclusion

The system demonstrates that ASP.NET Core MVC can be used effectively to build an organized and practical administrative application. By separating the application into controllers, models, services, views, and a database layer, the project remains easier to maintain and extend in the future.

In conclusion, the project is a functional and scalable traffic violation ticket management system that supports both administrative staff and public users in a practical and reliable way. It satisfies the main project requirements and provides a solid foundation for future improvement.