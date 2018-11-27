Inspired from https://github.com/Azure/iot-edge-v1/tree/master/v2/samples/azureiotedge-simulated-temperature-sensor
Few changes from existing - security is not implemented, the moduleClient is used instead of DeviceClient
to monitor events - use iothub-explorer monitor-events -l "HostName=xxxx.azure-devices.net;SharedAccessKeyNam
e=service;SharedAccessKey=xxxxxx" in any machine
