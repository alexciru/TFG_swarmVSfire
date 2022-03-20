# TFG_swarmVSfire

###### This project was carried out during the end of degree work at the Escuela Politecnica Superior at Autonomous University of Madrid.
|   |   |  
:-------------------------:|:-------------------------:
|<img src="Photos\EPS1.jpg" width="200" height="200"> |<img src="Photos\UAM1.png" width="200" height="200">  |



___

![Alt text](/Photos/UnityTraining.png)
### Main task

The objective of this final degree project is to study the feasibility of a fire monitoring system using drones. We have opted for the use of an architecture based on behaviors where instead of coding the overall system, simpler modules are coded that when interconnected achieve more complex behaviors to the system. In addition, for the codification of these behaviors, reinforcement learning techniques have been used to obtain more elaborate behaviors.

The algorithms have been developed and validated using a realistic forest fire simulation environment developed in-house. The results show how the aircraft can track the fire expansion obtaining information with a high degree of reliability with respect to the real fire growth. Additional simulations show that the approach can be scaled up by increasing the number of aircraft and the generalization of knowledge by being able to be applied to different fire silhouettes.



# Videos of the simulation available in the `Videos` folder.

### code structure

each behavior has been trained in a different scene 
- *BordearFuego_Training:*(Fire_Edge_Training) Fire_Edge behavior training
- *EsquivarObstaculo_Training:*(DodgeObstacle_Training) obstacle dodging behavior training
-  *SistemaGlobal:*(GlobalSystem) file where the complete system is located
- SampleScene:* scene of a 3d terrain with the system implemented

In each of the different scenes the execution will start after pressing the Play button. The simulations will show the results with the pre-trained models. In case you want to train a behavior you will have to execute a command from an external terminal with the command mlagents-learn. More information in the project repository and in the ML-Agents repository.

### Instalation
Download and install Unity (recommended version: 2019.3.15f )
´https://unity3d.com/get-unity/download/archive´

Once Unity is downloaded and installed, a new 3d project is created and all the files from the repository are imported.
It is necessary to download the following dependencies:

The following versions are recommended:
|Unity Packages |	version|
|---|---|
|com.unity.ml-agents (C#)	| v1.3.0|
|ml-agents (Python)	| v0.19.0|
|ml-agents-envs (Python)	 | v0.19.0|
|gym-unity (Python)	| v0.19.0|
|Communicator (C#/Python) |	v1.0.0|




# Path trajectory over time
<img src="Photos\path1.png" width="400" height="200">
<img src="Photos\path2.png" width="400" height="200">
<img src="Photos\path3.png" width="400" height="200">




