# 📡 Echo — Real-time Chat Application

**Echo** is a secure, real-time chat web application built using **ASP.NET MVC**, **SignalR**, **MSSQL**, and **Microsoft Azure Speech Services**. Developed as my graduation thesis for the Computer Engineering program, Echo enables users to send real-time messages, share multimedia, create and manage groups, and even convert speech to text. The project emphasizes accessibility, performance, and modern UI design — all without using frontend frameworks.

## 🔥 Features
- 💬 **Real-time Messaging** – Uses SignalR for instant message delivery without page refresh.
- 🎤 **Speech-to-Text** – Converts voice to text using Microsoft Azure Speech Services.
- 📂 **Multimedia Sharing** – Send images, documents, and more with validation and secure handling.
- 🧑‍🤝‍🧑 **Group Management** – Create groups, add/remove members, assign admin roles.
- ⭐ **Starred Messages** – Mark important messages for quick reference later.
- 🧑‍💻 **Authentication & Authorization** – Powered by ASP.NET Core Identity with secure login/register.
- 📱 **Responsive Design** – Works seamlessly across desktop, tablet, and mobile devices.
- 🔍 **Real-time Search** – Filter contacts and chats dynamically while typing.
- 🎨 **Audio Feedback & Accessibility** – Enhances UX through audio cues and high-contrast visual design.

## 🚧 Notes
- ⚠️ The application does not include end-to-end encryption (planned as future work).
- 🔒 Password requirements are minimal for testing purposes.
- 📦 Multimedia file support includes: `.jpg`, `.jpeg`, `.png`, `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.txt`.
- 🌐 Group chat and private chat support real-time message synchronization.

## 🛠️ Prerequisites
Ensure you have the following installed:
- [.NET SDK](https://dotnet.microsoft.com/en-us/download) (v7.0 or later recommended)
- [Visual Studio](https://visualstudio.microsoft.com/) with ASP.NET and web development workload
- SQL Server or LocalDB
- Internet connection (required for Azure Speech API)

## 📦 Installation

1. **Clone the Repository**
   ```bash
   git clone https://github.com/emirsapmaz/echo-chat-app.git
   cd echo-chat-app
   ```

2. **Open in Visual Studio**
   - Open the `.sln` file using Visual Studio.

3. **Configure Database**
   - Edit `appsettings.json`:
     ```json
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=EchoDB;Trusted_Connection=True;"
     }
     ```

4. **Apply Migrations & Seed Data**
   - Use Package Manager Console:
     ```sh
     Update-Database
     ```

5. **Set Up Azure Speech Keys (optional for speech-to-text)**
   - Add your subscription key and region in `appsettings.json`:
     ```json
     "AzureSpeech": {
       "SubscriptionKey": "YOUR_SPEECH_KEY",
       "Region": "YOUR_REGION"
     }
     ```

6. **Run the Application**
   - Use Visual Studio’s Run button or:
     ```sh
     dotnet run
     ```

🎉 That's it! Echo is now running on `https://localhost:xxxx`.

## 📚 Technologies Used
- ASP.NET Core MVC
- SignalR
- Entity Framework Core + MSSQL
- Microsoft Azure Speech Service
- HTML5, CSS3, JavaScript (no frameworks)
- Identity Core for Authentication

## 📈 Future Improvements
- 🔐 End-to-End Encryption
- 📞 Voice & Video Calling
- 🔔 Real-time Notifications
- ☁️ AWS S3 Integration for file storage
- 🤖 AI chatbot for smart replies and translation

## 📸 Screenshots
![Ekran görüntüsü 2025-06-01 191359](https://github.com/user-attachments/assets/9ea2950d-5920-4c5e-a096-904c2e652865)
![Ekran görüntüsü 2025-06-01 191441](https://github.com/user-attachments/assets/32848239-1915-4602-9bdf-384961d262ee)
![Ekran görüntüsü 2025-06-01 191546](https://github.com/user-attachments/assets/0383bf4c-4f92-443d-8a42-c41b26208d96)
![Ekran görüntüsü 2025-06-01 191610](https://github.com/user-attachments/assets/b3e0a909-8cd0-4b93-b0f7-0b7a6bfe02b6)
![Ekran görüntüsü 2025-06-01 191735](https://github.com/user-attachments/assets/5a2902d3-a2a2-402c-9cda-fc0153cae15c)


👨‍💻 Developed by Emir Sapmaz | [LinkedIn](https://linkedin.com/in/emirsapmaz)
