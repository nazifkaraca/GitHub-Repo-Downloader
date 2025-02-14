# **GitHub Repo Downloader**

## 📌 **Overview**
GitHub Repo Downloader is a **WPF-based GUI application** that allows users to **download specific folders from a GitHub repository**. Users can choose between:
- **API Mode** (using GitHub API for fetching files)
- **Non-API Mode** (using `git sparse-checkout` for downloading specific folders more efficiently)

This tool is ideal for developers who want to fetch specific project components **without cloning the entire repository**.

---

## **🔥 Features**
✅ **Download public & private repositories** (via API mode with a GitHub PAT)  

✅ **Switch between API and Non-API mode** seamlessly  

✅ **Supports GitHub URLs**: Directly parse URLs like  
   `https://github.com/user/repo/tree/main/path/to/folder`  

✅ **Folder Picker**: Choose where to save downloaded files  

✅ **Sparse-Checkout Optimization**: Ensures **only the selected folder is fetched**, avoiding unnecessary files  

✅ **Progress Tracking**: Live logs and progress updates while downloading  

✅ **Tree View Structure**: View downloaded files in a hierarchical format  

✅ **Minimal Dependencies**: Uses Octokit.NET for API requests and `git sparse-checkout` for API-less downloads  

---

## **🛠️ Installation**
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

## **🚀 Usage**
### **Step 1: Choose Mode**
- **API Mode:** Requires a **GitHub PAT (Personal Access Token)** to authenticate API requests.
- **Non-API Mode:** Uses `git sparse-checkout` to fetch files without authentication.

### **Step 2: Enter Repository Details**
- **GitHub Repository URL** *(Recommended: Paste the URL to auto-fill details)*
- **GitHub Username** (Repo Owner)
- **Repository Name**
- **Folder Path** (Relative path to the target folder inside the repo)
- **Branch Name** (Default: `main`)

### **Step 3: Select Download Location**
- Click `Browse` to choose a destination folder.

### **Step 4: Start Download**
- Click `Download` to begin fetching files.
- **Logs will update in real time**.

---

## **🔑 Generating a GitHub PAT**
If using **API Mode**, you must generate a **GitHub Personal Access Token (PAT)**:
1. Go to **[GitHub Token Settings](https://github.com/settings/tokens)**
2. Click **Generate New Token (classic)**
3. Select **Only the following scopes:**
   - ✅ `public_repo` (For public repos only)
   - ✅ `repo` (For both public and private repos)
4. Copy the generated token and enter it in the application.

⚠ **Never share your PAT! Keep it secure.**

---

## **🛠 Fixes & Improvements**
### ✅ **Fix: Git Sparse-Checkout Now Only Downloads the Selected Folder**
- Previously, Git would **pull extra files from the parent directory**.
- Now, **only the requested subfolder is fetched** using:
  ```sh
  git config core.sparseCheckout true
  git sparse-checkout set --no-cone "path/to/folder/*"
  ```
- **Ensures no unnecessary files are included**.

### ✅ **Fix: Spaces & Special Characters in Folder Paths**
- **Now correctly handles URLs with spaces** (`%20` is properly decoded).
- **Improved parsing** ensures valid GitHub folder paths.

### ✅ **Fix: Improved Git Command Execution**
- **Logs Git output and errors in real-time**, so users can debug issues easily.
- **Prevents crashes** due to incorrect directory paths.

---

## **🐞 Troubleshooting**
### **Issue: Git is not recognized**
**Solution:** Ensure Git is installed and added to the system PATH.
```sh
git --version
```
If it fails, reinstall Git from [git-scm.com](https://git-scm.com/).

### **Issue: API requests fail (403 Forbidden)**
**Solution:** Ensure your **GitHub PAT is valid and has the correct scopes**.

### **Issue: Empty download folder in Non-API mode**
**Solution:** Ensure Git sparse-checkout is correctly configured. Try manually running:
```sh
git config core.sparseCheckout true
git sparse-checkout set --no-cone "path/to/folder/*"
git checkout main
git sparse-checkout list
```
If this shows **no folders**, check the **folder path formatting**.

---

## **📜 License**
This project is licensed under the **AGPL-3.0 License**.

---

## **💡 Future Improvements**
- [ ] **Auto-update mechanism** for fetching the latest repo changes
- [ ] **MacOS/Linux support** using Avalonia UI
- [ ] **Better UI/UX with a progress bar**
- [ ] **Multi-folder selection for downloading multiple folders at once**

🚀 **Contributions are welcome!** If you find any issues or want to improve the project, feel free to submit a PR.
