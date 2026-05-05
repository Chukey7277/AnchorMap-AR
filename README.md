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
- 🌍 Outdoor AR anchoring using geospatial coordinates
- 💾 Persistent storage of anchors using Firebase Firestore
- ✍️ Contributor mode for placing anchors with metadata
- 🔎 Explorer mode for discovering and viewing stored anchors
- ⚙️ Readiness-aware interaction before placement/loading
- 📊 Evaluation logging for latency, tracking accuracy, and usability analysis

## 🛠️ Technologies Used

- Unity
- AR Foundation
- Google ARCore and ARCore Extensions
- MultiSet Platform
- Firebase Firestore
- C#
- Python for evaluation analysis

## 📂 Project Structure


Project/
│
├── Assets/
│   ├── Scripts/
│   ├── Scenes/
│   ├── Prefabs/
│   └── Resources/
│
├── Evaluation_files/
│   ├── evaluation logs
│   ├── generated summary tables
│   └── usability study files
│
└── README.md



## ⚙️ Setup Instructions (Brief)
1. Clone the repository:

git clone https://github.com/your-username/your-repo.git

2. Open the project in Unity (via Unity Hub).
3. Configure:

* Firebase Firestore (for data storage)
* ARCore (enable Geospatial API)
* MultiSet map (for indoor environments)

4. Build and run the project on an ARCore-supported Android device.


## 📱 Usage

### Contributor Mode
- Select **Contributor**
- Choose Indoor/Outdoor
- Wait for localisation
- Tap to place anchor
- Enter title & description

### Explorer Mode
- Select **Explorer**
- Wait for localisation
- View anchors
- Tap to open details

## 📊 Evaluation

The Evaluation_files/ folder contains evaluation-related files used for thesis analysis, including recorded logs and usability study material.

The system logs interaction data in CSV format, including:

* Latency (latency_ms)
* Tracking accuracy (earth_hacc_m, earth_vacc_m, earth_headacc_deg)
* Success/failure events
* Anchor placement events
These logs were used for performance evaluation and usability analysis in the thesis.

## 📌 Limitations

* Outdoor accuracy depends on ARCore Earth tracking quality.
* Indoor operation requires pre-scanned MultiSet maps.
* The current implementation supports text-based metadata only.

## 🔮 Future Work

* Multimedia anchor support such as images and audio
* Navigation assistance toward anchors
* Improved tracking robustness
* Multi-user collaboration and moderation features

## 👤 Author

Tsewang Chukey
MTech Thesis
Indian Institute of Technology Kanpur

## 📄 License

This project is intended for academic use. Please contact the author for reuse or extension.
