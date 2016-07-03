// SensorDeviceWrapper.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "SensorDeviceWrapper.h"
#include "DeviceController.h"

#include <stdio.h>
#include <iostream>
// This is an example of an exported variable
SENSORDEVICEWRAPPER_API int nSensorDeviceWrapper=0;

// This is an example of an exported function.
SENSORDEVICEWRAPPER_API int fnSensorDeviceWrapper(void)
{
    return 42;
}

// This is the constructor of a class that has been exported.
// see SensorDeviceWrapper.h for the class definition
CSensorDeviceWrapper::CSensorDeviceWrapper()
{
    return;
}
int CSensorDeviceWrapper::testDevice() {
	if (!initSensor()) {
		printf("init failed\n");
		return 0;
	}
	for (int i = 0; i < 50; i++)
	{
		double t = getCalibratedData();
		printf("Read Data = %f\t", t);
		Sleep(1000);
	}
	closeSensor();
	return 1;
}
void DebugOut(char* t) {
	OutputDebugStringA(t);
}

SENSORDEVICEWRAPPER_API int testDevice2() {
	char sBuf[1024];
	if (!initSensor()) {
		printf("init failed\n");
		return 0;
	}
	for (int i = 0; i < 50; i++)
	{
		double t = getCalibratedData();
		printf("Read Data = %f\t", t);
		sprintf_s(sBuf, "Read Data = %f\t", t);
		DebugOut(sBuf);


		Sleep(1000);
	}
	closeSensor();
	return 1;
}


SENSORDEVICEWRAPPER_API int InitializeSensor() {
	if (!initSensor()) {
		printf("init failed\n");
		return 0;
	}
	return 1;
}
SENSORDEVICEWRAPPER_API int UnInitializeSensor() {
	closeSensor();
	return 1;
}
SENSORDEVICEWRAPPER_API double readCalibratedData() {
	//return getCalibratedData();
	return getCalibratedDatafromMultiMeasures();

}





