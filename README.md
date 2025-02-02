# GitHub Repo Downloader

## 📌 Overview
GitHub Repo Downloader is a **WPF-based GUI application** that allows users to **download specific folders from a GitHub repository**. Users can choose between:
- **API Mode** (using GitHub API for fetching files)
- **Non-API Mode** (using `git sparse-checkout` for downloading specific folders)

This tool is ideal for developers who want to fetch specific project components without cloning the entire repository.

---

## 🔥 Features
✅ **Download public & private repositories** (via API mode with a GitHub PAT)
✅ **Switch between API and Non-API mode** seamlessly
✅ **Folder Picker**: Choose where to save downloaded files
✅ **Progress Tracking**: Live progress updates while downloading
✅ **Tree View Structure**: View downloaded files in a hierarchical format
✅ **Minimal Dependencies**: Uses Octokit.NET for API requests and `git sparse-checkout` for API-less downloads

---

## 🛠️ Installation
### **Prerequisites**
- **Windows OS** (Required for WPF support)
- **.NET 6/7/8 SDK** ([Download here](https://dotnet.microsoft.com/en-us/download/dotnet))
- **Git Installed** (For Non-API mode) ([Download Git](https://git-scm.com/downloads))

### **Clone the Repository**
```sh
git clone https://github.com/nazifkaraca/GitHub-Repo-Downloader.git
cd GitHub-Repo-Downloader
```

### **Build and Run**
```sh
dotnet build
dotnet run
```

---

## 🚀 Usage
### **Step 1: Choose Mode**
- **API Mode:** Requires a **GitHub PAT (Personal Access Token)** to authenticate API requests.
- **Non-API Mode:** Uses `git sparse-checkout` to fetch files without authentication.

### **Step 2: Enter Repository Details**
- **GitHub Username** (Repo Owner)
- **Repository Name**
- **Folder Path** (Relative path to the target folder inside the repo)
- **Branch Name** (Default: `main`)

### **Step 3: Select Download Location**
- Click `Browse` to choose a destination folder.

### **Step 4: Start Download**
- Click `Download` to begin fetching files.
- **Progress bar and logs will update in real time**.

---

## 🔑 Generating a GitHub PAT
If using **API Mode**, you must generate a **GitHub Personal Access Token (PAT)**:
1. Go to **[GitHub Token Settings](https://github.com/settings/tokens)**
2. Click **Generate New Token (classic)**
3. Select **Only the following scopes:**
   - ✅ `public_repo` (For public repos only)
   - ✅ `repo` (For both public and private repos)
4. Copy the generated token and enter it in the application.

⚠ **Never share your PAT! Keep it secure.**

---

## 🐞 Troubleshooting
### **Issue: Git is not recognized**
**Solution:** Ensure Git is installed and added to the system PATH.
```sh
git --version
```
If it fails, reinstall Git from [git-scm.com](https://git-scm.com/).

### **Issue: API requests fail (403 Forbidden)**
**Solution:** Ensure your **GitHub PAT is valid and has the correct scopes**.

### **Issue: Empty download folder in Non-API mode**
**Solution:** Some repositories require `git sparse-checkout` to be configured properly. Try manually running:
```sh
git sparse-checkout set path/to/folder
```

---

## 📜 License
This project is licensed under the **MIT License**. Feel free to use and modify!

---

## 💡 Future Improvements
- [ ] **Auto-update mechanism** for fetching the latest repo changes
- [ ] **MacOS/Linux support** using Avalonia UI
- [ ] **Download progress with estimated time remaining**

Contributions are welcome! If you find any issues or want to improve the project, feel free to submit a PR. 🚀

