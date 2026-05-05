# Indoor–Outdoor Augmented Reality Anchoring System
This project implements a unified mobile augmented reality (AR) system that enables users to create, store, and explore persistent location-based information across both indoor and outdoor environments.
The system integrates:
- **Indoor map-based localisation (MultiSet)**
- **Outdoor geospatial anchoring (ARCore Earth API)**
- **Cloud-based persistence (Firebase Firestore)**
- **User workflows for contributor and explorer interaction**
This work was developed as part of an MTech thesis at IIT Kanpur.

## 🚀 Features
- 📍 Indoor AR anchoring using pre-scanned map environments
- 🌍 Outdoor AR anchoring using geospatial coordinates (latitude, longitude, altitude)
- 💾 Persistent storage of anchors using Firebase Firestore
- ✍️ Contributor mode for placing anchors with metadata (title & description)
- 🔎 Explorer mode for discovering and viewing stored anchors
- ⚙️ Readiness-aware interaction (ensures stable localisation before interaction)
- 📊 Evaluation logging for latency, tracking accuracy, and usability analysis

## 🧱 System Architecture
The system consists of the following components:
1. **Unity Mobile Application**
   - AR interaction
   - UI workflows (contributor & explorer)
   - Anchor placement and reconstruction
2. **Indoor Localisation (MultiSet)**
   - Map-based localisation
   - VPS (Visual Positioning System)
3. **Outdoor Localisation (ARCore Earth)**
   - Geospatial tracking
   - Earth-referenced anchor placement
4. **Cloud Backend (Firebase Firestore)**
   - Anchor data storage
   - Metadata persistence
   - Cross-session retrieval

## 🛠️ Technologies Used
- Unity (AR Foundation)
- Google ARCore + ARCore Extensions
- MultiSet Platform (Indoor VPS)
- Firebase Firestore
- C# (Unity scripting)
- Python (evaluation analysis)
  
## 📂 Project Structure

Project/
│
├── Assets/
│   ├── Scripts/
│   │   ├── AnchorData.cs
│   │   ├── AnchorPlacer.cs
│   │   ├── LocalizationManager.cs
│   │   └── UIControllers/
│   │
│   ├── Scenes/
│   ├── Prefabs/
│   └── Resources/
│
└── README.md


## ⚙️ Setup Instructions
### 1. Clone the repository
```bash
git clone https://github.com/your-username/your-repo.git



2. Open in Unity

* Open Unity Hub
* Select Open Project
* Choose the project folder



3. Configure Firebase

* Create a Firebase project
* Enable Firestore Database
* Replace Firebase configuration in the project
* Ensure read/write rules are properly set for testing



4. Configure ARCore

* Enable ARCore support in Unity
* Install ARCore Extensions
* Enable Geospatial API
* Add API key in project settings



5. Indoor Setup (MultiSet)

* Prepare indoor map using MultiSet tools
* Upload map to MultiSet cloud
* Use map ID inside the app



6. Build and Run

* Connect Android device (ARCore-supported)
* Build APK from Unity
* Install and run on device



📱 Usage

Contributor Mode

1. Select mode → Contributor
2. Choose environment → Indoor / Outdoor
3. Wait for localisation readiness
4. Tap on screen to place anchor
5. Enter title and description
6. Save anchor



Explorer Mode

1. Select mode → Explorer
2. Choose environment
3. Wait for localisation
4. View existing anchors
5. Tap anchor to view details



📊 Evaluation

The system logs interaction data in CSV format, including:

* Latency (latency_ms)
* Tracking accuracy (earth_hacc_m, earth_vacc_m, earth_headacc_deg)
* Success/failure events
* Anchor placement events

These logs were used for performance evaluation and usability analysis in the thesis.



📌 Limitations

* Outdoor accuracy depends on ARCore Earth tracking quality
* Indoor operation requires pre-scanned maps
* Currently supports text-based metadata only



🔮 Future Work

* Multimedia anchor support (images/audio)
* Navigation assistance to anchors
* Improved tracking robustness
* Multi-user collaboration features



👤 Author

Tsewang Chukey
MTech Thesis 
Indian Institute of Technology Kanpur



📄 License

This project is intended for academic use. Please contact the author for reuse or extension.


