<p align="center">
  <img alt="SoundBoosterAppBanner" src="https://i.imgur.com/8kyswxi.jpeg">
<br>
  <img alt="GitHub Release" src="https://img.shields.io/github/v/release/RGB-Outl4w/Sound-Booster-App?logo=github">
  <img alt="GitHub repo size" src="https://img.shields.io/github/repo-size/RGB-Outl4w/Sound-Booster-App">
  <img alt="GitHub Downloads (all assets, all releases)" src="https://img.shields.io/github/downloads/RGB-Outl4w/Sound-Booster-App/total?color=lime">
  <img alt="GitHub Repo stars" src="https://img.shields.io/github/stars/RGB-Outl4w/Sound-Booster-App?style=flat&color=yellow">
</p>

# 🔊 Sound Booster App

The Sound Booster App provides fine-grained control over application-specific microphone volume levels on Windows systems. Designed for users seeking to enhance their audio experience without affecting the overall system volume, this utility allows for tailored audio boosting on a per-application basis.


**🔑 Key Features:**

* **Granular Volume Control:** Assign specific boost levels to individual applications, customizing your audio experience for specific tasks such as gaming, conferencing, or streaming.
* **Intuitive User Interface:** The straightforward design facilitates effortless navigation and configuration of application boost settings.
* **Persistent Configuration:**  Retains custom boost settings for each application, eliminating the need for repeated adjustments. 
* **Minimal System Footprint:** Designed for minimal resource utilization, ensuring smooth system performance even with multiple applications running. 

**⁉️ How it Works:**

The application leverages the Windows Audio Session API (WASAPI) to intercept and modify the audio stream from your chosen microphone. You can route this boosted audio through any output device, including virtual audio cables, granting you flexibility for complex audio setups. 

**🔍 Use Cases:**

* Enhance microphone clarity in online games without impacting in-game volume levels.
* Ensure clear and audible communication during video conferences without needing to adjust overall system settings.
* Optimize microphone input volume for specific streaming or recording software for higher quality output.
* Customize audio levels for accessibility purposes.

**📥 Installation & Usage:**

0. Set up your [virtual audio cable](https://golightstream.com/how-to-setup-virtual-audio-cables/)
1. Download the latest release from the `Releases` page.
2. Extract the ZIP archive and run the executable file. 
3. Select your desired input and output audio devices upon launching the application.
* 3.1 Both of the devices should be microphones. The input being your physical microphone and the output being your VAC.
4. Choose the target application from the provided list.
5. Adjust the boost slider to your preferred amplification level. 

**⚙️ Technical Details:**

* Developed using C# and Windows Forms.
* Leverages the NAudio library for audio processing.
* Configuration files are stored in JSON format for ease of management.

**🤝 Contributions:**

We encourage contributions to the project! If you encounter a bug, have feature requests, or would like to improve the codebase, feel free to open an issue or submit a pull request through the GitHub repository.

...And don't forget about following the license!

## F.A.Q (Frequently Asked Questions)

* If you have any unanswered questions, check the [FAQ tab](https://gist.github.com/RGB-Outl4w/a05a7410d32ea41aa260a55b11ceb70e)!
## 💖 Supporting the Developer

This project is free and open-source, developed and maintained in my spare time to help others achieve better audio control. If you find this application useful and would like to support its continued development, any contribution is greatly appreciated. You can support me through:

* [🧡 My boosty page](https://boosty.to/rgboutlaw)

Thank you for your generosity!
