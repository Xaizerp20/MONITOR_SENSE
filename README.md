# Proyecto MONITOR-SENSE: Lectura de Estados de Habitaciones mediante RFID y Comunicación MQTT en C#

Este repositorio contiene el código y los recursos necesarios para el proyecto MONITOR-SENSE en C#. Este proyecto permite la lectura de los estados de habitaciones utilizando tecnología RFID a través de entradas digitales, empleando el protocolo I2C para la comunicación, y luego enviando los datos capturados a un broker MQTT. Esta combinación de tecnologías permite crear un sistema eficiente para la captura y transferencia de datos de los estados de habitaciones a través de una red MQTT.

## Características

- Lectura de los estados de habitaciones mediante tecnología RFID y entradas digitales.
- Comunicación eficiente mediante el protocolo I2C.
- Envío de los datos de los estados de habitaciones a un broker MQTT.
- Potencial para integración con sistemas de automatización y control.
  
## Requisitos Previos

- [SDK de .NET](https://dotnet.microsoft.com/download/dotnet) instalado en tu sistema.

## Instrucciones de Uso

1. **Clonar el Repositorio**: Clona este repositorio en tu máquina local:

   ```bash
   git clone https://github.com/tu-usuario/tu-proyecto.git


## Configuración 
  - Abre el proyecto en tu entorno de desarrollo y configura los valores necesarios en el archivo de configuración.

## Compilación: 
  - Compila el proyecto y asegúrate de que no haya errores.
  ```bash
  dotnet build
  ```

## Ejecución: 
  - Ejecuta la aplicación desde tu entorno de desarrollo o utilizando el comando:
  ```bash
  dotnet run
  ```
La aplicación comenzará a leer las tarjetas RFID a través de las entradas digitales configuradas y enviará los datos al broker MQTT especificado en la configuración.

## Estructura del Proyecto

El proyecto está organizado de la siguiente manera:

- `Program.cs`: Archivo principal que contiene el punto de entrada de la aplicación.
- `Configuration.cs`: Archivo que maneja la configuración de comunicación MQTT y las entradas digitales.
- `I2cExpander.cs`: Clase que gestiona la comunicación I2C.
- `Room`: Clase para leer los datos de las habitaciones
- `MQTT.cs`: Clase que maneja la conexión y el envío de datos al broker MQTT.
