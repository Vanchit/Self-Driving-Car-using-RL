# Self-Driving Car Using Reinforcement Learning

A Unity-based 3D autonomous-driving simulation project exploring reinforcement learning for track following, obstacle avoidance, and checkpoint navigation.

> **Note:** Update this README with the exact Unity version, training command, and any project-specific setup once verified against the files in this repository.

## Project structure

This repository is expected to contain a Unity project with these main folders:

- `Assets/` — scenes, scripts, prefabs, materials, and other project assets.
- `Packages/` — Unity package manifest and package dependencies.
- `ProjectSettings/` — Unity project configuration.
- `config/` — project or training configuration files, if used.
- `results/` — training outputs or evaluation results, if included.

## Requirements

- **Unity Hub** and the Unity Editor version specified in `ProjectSettings/ProjectVersion.txt`.
- Git, to clone the repository.
- If the project uses Unity ML-Agents training: the compatible Python version and ML-Agents packages required by the project's configuration.

Do not install arbitrary versions of ML-Agents or Python without checking compatibility with the Unity package version used by this project.

## 1. Download the project

Clone the repository:

```bash
git clone https://github.com/Vanchit/Self-Driving-Car-using-RL.git
cd Self-Driving-Car-using-RL
```

Alternatively, use **Code → Download ZIP** on GitHub and extract the archive.

## 2. Open the Unity project

1. Open **Unity Hub**.
2. Select **Add** (or **Add project from disk**).
3. Select the cloned project folder — the folder containing `Assets`, `Packages`, and `ProjectSettings`.
4. Install and open the Unity Editor version specified in `ProjectSettings/ProjectVersion.txt`.
5. Allow Unity to import assets and resolve packages. The first opening may take several minutes.
6. In the Unity Project window, locate the main scene under `Assets/` and open it. The exact scene name depends on the files included in the repository.
7. Press **Play** to run the simulation.

If Unity reports missing packages or scripts, check the Console and install only the dependencies required by this project.

## 3. Run reinforcement-learning training

Training instructions depend on the agent scripts and ML-Agents configuration included in the project.

1. Open the project in the compatible Unity Editor.
2. Check `Packages/manifest.json` for the installed ML-Agents Unity package version, if applicable.
3. Locate the training configuration files under `config/` and identify the agent behavior name.
4. Install the matching Python ML-Agents package and its compatible dependencies in a virtual environment, following the official Unity ML-Agents installation documentation.
5. Run the training command using the actual configuration filename and behavior name from this repository.

**Example command format only** (replace the placeholder with the real configuration file):

```bash
mlagents-learn config/<training-config>.yaml --run-id=SelfDrivingCar
```

This example will not work until the correct configuration path exists and compatible ML-Agents dependencies are installed. If the project only runs a pre-trained agent or uses a custom training script, follow that implementation instead.

## 4. View results

- Run the simulation in Unity and inspect the agent's driving behavior.
- Check the `results/` folder for any included training logs, models, or evaluation outputs.
- If a trained model is included, use the behavior/model setup configured by the project scripts.

## Troubleshooting

- **Unity version mismatch:** Open `ProjectSettings/ProjectVersion.txt` and install the listed editor version through Unity Hub.
- **Packages fail to resolve:** Check `Packages/manifest.json` and the Unity Console for the specific missing package.
- **Training command fails:** Verify the configuration file path, behavior name, Python version, and ML-Agents version compatibility.
- **No scene is obvious:** Browse `Assets/` for Unity scene files (`.unity`) and open the intended scene manually.
- **Project opens but the car does not move:** Check the Console for missing scripts, agent components, behavior settings, model assignments, or training connection requirements.

## Technologies

- Unity
- C#
- Python
- Reinforcement Learning
- Unity ML-Agents (if enabled by the project configuration)

## Author / Links

- **Repository:** https://github.com/Vanchit/Self-Driving-Car-using-RL

Add the project owner's name, contact information, demo video, screenshots, and any related documentation here.


