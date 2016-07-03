// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the SENSORDEVICEWRAPPER_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// SENSORDEVICEWRAPPER_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef SENSORDEVICEWRAPPER_EXPORTS
#define SENSORDEVICEWRAPPER_API __declspec(dllexport)
#else
#define SENSORDEVICEWRAPPER_API __declspec(dllimport)
#endif

// This class is exported from the SensorDeviceWrapper.dll
class SENSORDEVICEWRAPPER_API CSensorDeviceWrapper {
public:
	CSensorDeviceWrapper(void);
	// TODO: add your methods here.

	static int testDevice();
};

extern "C" SENSORDEVICEWRAPPER_API int nSensorDeviceWrapper;

extern "C" SENSORDEVICEWRAPPER_API int fnSensorDeviceWrapper(void);

extern "C" SENSORDEVICEWRAPPER_API int testDevice2(void);

extern "C" SENSORDEVICEWRAPPER_API int InitializeSensor();
extern "C" SENSORDEVICEWRAPPER_API int UnInitializeSensor();
extern "C" SENSORDEVICEWRAPPER_API double readCalibratedData();