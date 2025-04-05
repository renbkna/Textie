# Text Spammer

A simple but visually enhanced **text spamming tool** built with .NET and Spectre.Console. It allows users to send repeated messages automatically to any text input field (such as a chat window).

## Features

- **Global Keyboard Hook**: Listens for `Enter` key (to start spamming) and `Escape` key (to stop spamming) presses globally using Windows API.
- **Rich Console UI**: Uses `Spectre.Console` for interactive prompts, instructions, progress visualization, and a visually appealing user experience.
- **Customizable Input**: Allows full configuration of the message to spam, the number of repetitions, and the delay between each message in milliseconds.
- **Multithreaded Execution**: Runs the spamming process in a background thread to ensure the console UI remains responsive and interactive even during active spamming.
- **Windows Input Simulation (via WindowsInput Library)**: Leverages the `WindowsInput` library (specifically `InputSimulator`) to accurately simulate keyboard input events on Windows.

## How It Works

1. Run the program using `dotnet run`.
2. The program will prompt you via `Spectre.Console` to enter:
    - The message you wish to spam.
    - The number of times you want to send the message.
    - The delay in milliseconds between each message.
3. Carefully read the instructions presented in the bordered panel on the console.
4. Switch focus to the application where you intend to spam text (e.g., a chat window, text document, etc.). **Ensure the target application is focused.**
5. **Press Enter** on your keyboard to initiate the spamming process with the configured settings.
6. A progress bar, powered by `Spectre.Console`, will visually display the spamming progress.
7. **Press Escape** at any moment to immediately halt the spamming execution.

---

## Installation & Setup

### Prerequisites

- **Windows OS** (Required as the program utilizes Windows API for global keyboard hooks and relies on the `WindowsInput` library, which is Windows-specific for input simulation).
- **.NET SDK 7.0** (or later) - Ensure you have the .NET SDK installed on your system.

### Steps to Build & Run

1. **Clone the Repository**

    ```sh
    git clone https://github.com/renbkna/text-spammer
    cd text-spammer
    ```

2. **Run the Application**

    Open your terminal or command prompt within the `text-spammer` directory and execute:

    ```sh
    dotnet run
    ```

    This command will handle dependency restoration, compilation, and execution of the application in one step.

---

## Usage Instructions

1. Launch the **Text Spammer** application by running `dotnet run` in the project directory.
2. Follow the interactive prompts in the console to input your desired spam message, the number of times to repeat it, and the delay between messages.
3. Bring the application where you want to spam text into focus (make sure it's the active window).
4. **Press Enter** to begin the automated text spamming.
5. **Press Escape** at any point if you need to stop the spamming immediately.

---

## Known Issues & Troubleshooting

- **Keyboard hook not functioning:** If the program does not respond to the Enter or Escape keys, try **re-running the terminal or command prompt as an administrator**. Global keyboard hooks often require elevated privileges to function correctly.
- **Text spam not appearing in certain applications:** Some applications, particularly games or applications with robust anti-automation or anti-cheat mechanisms, may block simulated keyboard inputs from external programs. This includes tools like Text Spammer that use `WindowsInput`.  For such cases, consider exploring more advanced input simulation techniques like direct WinAPI `SendInput` or libraries like `InputSimulatorPlus` (though compatibility is never guaranteed, and some applications are intentionally designed to resist any form of automated input).
- **Windows OS Dependency:** This application is built specifically for Windows and relies on Windows-specific APIs and libraries. It is **not cross-platform** and will not run on other operating systems without significant modifications.

---

## Legal Disclaimer

This software is provided for **educational and testing purposes exclusively**. Any misuse of this program, including but not limited to spamming, harassment, or violation of terms of service of any online platform or application, is **strictly prohibited and is the sole responsibility of the user**. The developers are not liable for any consequences arising from inappropriate or illegal use of this tool.

---

## Future Improvements

- Investigate and potentially implement more robust and cross-platform compatible input simulation methods (acknowledging that true cross-platform global input simulation is inherently complex).
- Add functionality for randomized delays between messages to simulate more human-like typing patterns.
- Implement mechanisms to prevent the accidental or intentional running of multiple instances of the application simultaneously.
- (Potentially) Explore developing a Graphical User Interface (GUI) version in the future, although the current focus is on refining the console-based experience.

---

## License

This project is distributed under the **MIT License**. Refer to the `LICENSE` file for complete license details.
