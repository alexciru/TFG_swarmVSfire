# TFG_swarmVSfire

###### Este proyecto fue realizado durante el trabajo de fin de grado de la Escuela Politecnica Superior de la UAM.
![Logo Eps](https://www.uam.es/EPS/imagen/1242659838616/logo2.jpg)
![Logo UAM](https://logos-marcas.com/wp-content/uploads/2020/03/UAM-Logo.png)

___

![Alt text](/Photos/UnityTraining.png)
### Main task

Este trabajo de fin de grado tiene como objetivo estudiar la viabilidad de un sistema de monitorización de incendios haciendo uso de drones. Se ha optado por el uso de una arquitectura basada en comportamientos en donde en vez de codificar el sistema global se codifican módulos más sencillos que al ser interconectados logran conductas más complejas al sistema. Además, para la codificación de estos comportamientos se han utilizado técnicas de aprendizaje por refuerzo para la obtención de funcionamientos más elaborados.

Los algoritmos se han desarrollado y validado mediante un entorno de simulación de incendios forestales realista desarrollado en el propio trabajo. Los resultados muestran cómo las aeronaves pueden realizar un seguimiento de la expansión del incendio obteniendo información con un alto grado de fiabilidad respecto al crecimiento del incendio real. Simulaciones adicionales demuestran que el planteamiento se puede escalar aumentado el número de aeronaves y la generalización del conocimiento al poder ser aplicado en diferentes siluetas de incendio.


## poner gif simulacion
![Alt text](https://media.giphy.com/media/l41lUJ1YoZB1lHVPG/giphy.gif)

# Videos de la simulacion disponibles en la carpteta `Videos`

### Estructura del codigo

cada comportamiento se ha entrenado en una escena difertente 
- *BordearFuego_Training:* Entrenamiento del comportamiento Bordear Fuego
- *EsquivarObstaculo_Training:* Entrenamiento comportamiento esquivar obstaculo
- *SistemaGlobal:* archivo donde se encuentra el sistema completo
- *SampleScene:* escena de un terreno 3d con el sistema implantado

En cada una de las diferentes escenas la ejecución empezará tras pulsar el botón de Play. Las simulaciones se mostrará los resultados con los modelos pre-entrenados. En caso de querer entrenar un comportamiento deberá ejecutar un comando desde una terminal externa con el comando mlagents-learn. Más información en el repositorio del proyecto y en repositorio de ML-Agents.

### Instalacion
Descargar e instalar Unity (version recomendada: 2019.3.15f )
´https://unity3d.com/get-unity/download/archive´

Una vez descargado e instalado Unity se crea un nuevo proyecto 3d y se importa todos los archivos del repositorio.
Es necesario descargar las siguientes dependencias:

Se recomienda las siguientes versiones:
|Paquetes Unity |	versión|
|---|---|
|com.unity.ml-agents (C#)	| v1.3.0|
|ml-agents (Python)	| v0.19.0|
|ml-agents-envs (Python)	 | v0.19.0|
|gym-unity (Python)	| v0.19.0|
|Communicator (C#/Python) |	v1.0.0|


