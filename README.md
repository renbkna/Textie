# Text Spammer

A simple but advanced **text spamming tool** that allows users to send repeated messages automatically to any text input field (such as a chat window).

## Features

- **Global Keyboard Hook**: Listens for key presses to start/stop spamming.

- **Customizable Input**: Allows the user to enter a message, repetition count, and delay.

- **Multithreaded Execution**: Runs the spamming process in a separate thread.

- **Lightweight & Console-Based**: Simple and efficient with minimal system overhead.

## How It Works

1. Run the program.
2. Enter the message to be spammed.
3. Enter the number of times the message should be sent.
4. Enter the delay (in milliseconds) between messages.
5. **Press Enter** to start spamming.
6. **Press Escape** to stop spamming.
7. Ensure the target application (e.g., a chat box) is in focus.

---

## Installation & Setup

### Prerequisites

- **Windows OS** (Due to Windows API dependencies)

- **Visual Studio (2019 or later)**

- **.NET Framework (4.7 or later recommended)**

### Steps to Build & Run

1. **Clone the Repository**

   ```sh
   git clone https://github.com/renbkna/text-spammer
   cd text-spammer
   ```

2. **Open the Project in Visual Studio**

   - Launch **Visual Studio**.
   - Open the `spam.sln` file.

3. **Add Required References** (if not already present)

   - Right-click the project in **Solution Explorer**.
   - Select **Add Reference…**
   - Ensure **System.Windows.Forms** is included.

4. **Build the Solution**

   - Press `Ctrl + Shift + B` or go to **Build > Build Solution**.

5. **Run the Application**

   - Press `F5` or go to **Debug > Start Debugging**.
   - Follow the on-screen instructions.

---

## Usage Instructions

1. Open any text input field (e.g., chat box, text editor).

2. Run the **Text Spammer**.

3. Enter the message, spam count, and delay.

4. **Press Enter** to start the spamming process.

5. **Press Escape** at any time to stop spamming.

---

## Known Issues & Troubleshooting

### "Keyboard hook not working"

- Try **running the program as an administrator**.

### "Spam not appearing in some applications"

- Some applications may block `SendKeys.SendWait()`. Try using a different method like **AutoHotkey** for bypassing restrictions.

### "Multiple instances running"

- The program does not prevent multiple instances. Close any duplicate processes.

---

## Legal Disclaimer

This program is intended for **educational purposes only**. Misuse, including spamming, harassment, or violating terms of service of any application, is **strictly prohibited**. Use responsibly.

---

## Future Improvements

- Add a **Graphical User Interface (GUI)**.

- Support for **randomized delays** to simulate human-like typing.

- Multi-key sequence support.

---

## License

This project is licensed under the **MIT License**. See `LICENSE` for details.
