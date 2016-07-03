
#include "stdafx.h"
#include <stddef.h>
#include <stdio.h>
#include <string.h>
#include <memory.h>
#include <iostream>
#include <fstream>
#include <ctime>
#include <string>
#include <sstream>



#include "DeviceController.h"


#include "GoIO_DLL_interface.h"
#define MAX_NUM_MEASUREMENTS 100


GOIO_SENSOR_HANDLE openSensor(char * deviceName, gtype_int32 vendorId, gtype_int32 productId);
bool GetAvailableDeviceName(char *deviceName, gtype_int32 nameLength, gtype_int32 *pVendorId, gtype_int32 *pProductId);
//gtype_real64 *getDataArray(GOIO_SENSOR_HANDLE hDevice, gtype_real64 MeasurementPeriod);
gtype_real64 getLatestData(GOIO_SENSOR_HANDLE hDevice);


using namespace std;
char *deviceDesc[8] = { "?", "?", "Go! Temp", "Go! Link", "Go! Motion", "?", "?", "Mini GC" };

GOIO_SENSOR_HANDLE hSensor = NULL;

bool GetAvailableDeviceName(char *deviceName, gtype_int32 nameLength, gtype_int32 *pVendorId, gtype_int32 *pProductId)
{
	bool bFoundDevice = false;
	deviceName[0] = 0;

	/*Sample code indicates checking for various types of Go! devices so I left this segment of
	code since I was unsure what type of Go device I used.*/
	int numSkips = GoIO_UpdateListOfAvailableDevices(VERNIER_DEFAULT_VENDOR_ID, SKIP_DEFAULT_PRODUCT_ID);
	int numJonahs = GoIO_UpdateListOfAvailableDevices(VERNIER_DEFAULT_VENDOR_ID, USB_DIRECT_TEMP_DEFAULT_PRODUCT_ID);
	int numCyclopses = GoIO_UpdateListOfAvailableDevices(VERNIER_DEFAULT_VENDOR_ID, CYCLOPS_DEFAULT_PRODUCT_ID);
	int numMiniGCs = GoIO_UpdateListOfAvailableDevices(VERNIER_DEFAULT_VENDOR_ID, MINI_GC_DEFAULT_PRODUCT_ID);

	if (numSkips > 0)
	{
		GoIO_GetNthAvailableDeviceName(deviceName, nameLength, VERNIER_DEFAULT_VENDOR_ID, SKIP_DEFAULT_PRODUCT_ID, 0);
		*pVendorId = VERNIER_DEFAULT_VENDOR_ID;
		*pProductId = SKIP_DEFAULT_PRODUCT_ID;
		bFoundDevice = true;
	}
	else if (numJonahs > 0)
	{
		GoIO_GetNthAvailableDeviceName(deviceName, nameLength, VERNIER_DEFAULT_VENDOR_ID, USB_DIRECT_TEMP_DEFAULT_PRODUCT_ID, 0);
		*pVendorId = VERNIER_DEFAULT_VENDOR_ID;
		*pProductId = USB_DIRECT_TEMP_DEFAULT_PRODUCT_ID;
		bFoundDevice = true;
	}
	else if (numCyclopses > 0)
	{
		GoIO_GetNthAvailableDeviceName(deviceName, nameLength, VERNIER_DEFAULT_VENDOR_ID, CYCLOPS_DEFAULT_PRODUCT_ID, 0);
		*pVendorId = VERNIER_DEFAULT_VENDOR_ID;
		*pProductId = CYCLOPS_DEFAULT_PRODUCT_ID;
		bFoundDevice = true;
	}
	else if (numMiniGCs > 0)
	{
		GoIO_GetNthAvailableDeviceName(deviceName, nameLength, VERNIER_DEFAULT_VENDOR_ID, MINI_GC_DEFAULT_PRODUCT_ID, 0);
		*pVendorId = VERNIER_DEFAULT_VENDOR_ID;
		*pProductId = MINI_GC_DEFAULT_PRODUCT_ID;
		bFoundDevice = true;
	}

	return bFoundDevice;
}


/*This function open the sensor so that it is readily available to have data pulled from the
sensor's buffer. It returns the Sensor's handle which can then be used in other data usage methods to stream the data.*/
GOIO_SENSOR_HANDLE openSensor(char * deviceName, gtype_int32 vendorId, gtype_int32 productId) {

	char tmpstring[100];

	GOIO_SENSOR_HANDLE hDevice = NULL;
	bool bFoundDevice = GetAvailableDeviceName(deviceName, GOIO_MAX_SIZE_DEVICE_NAME, &vendorId, &productId);

	
	hDevice = GoIO_Sensor_Open(deviceName, vendorId, productId, 0);
		int nTry = 10;
		while (hDevice == NULL && nTry>0) {
			printf("Opening device");
			hDevice = GoIO_Sensor_Open(deviceName, vendorId, productId, 0);
			Sleep(500);
			nTry--;

		}
		if (nTry == 0) {
			cout << "[BreathingPhaseSensor] Failed to open sensor device\n";

			return NULL;
		}
		/*If the device can be opened the information about the sensor will be output.*/
		if (hDevice != NULL)
		{

			printf("Successfully opened %s device %s .\n", deviceDesc[productId], deviceName);

			unsigned char charId;
			GoIO_Sensor_DDSMem_GetSensorNumber(hDevice, &charId, 0, 0);
			printf("Sensor id = %d", charId);

			GoIO_Sensor_DDSMem_GetLongName(hDevice, tmpstring, sizeof(tmpstring));
			if (strlen(tmpstring) != 0)
				printf("(%s)", tmpstring);
			printf("\n");

		}
	
	return hDevice;
}



/*This method returns an array of all the data read from the sensor until the maximum number of
measurements is read.*/
/*gtype_real64 * getDataArray(GOIO_SENSOR_HANDLE hDevice, gtype_real64 MeasurementPeriod) {
	gtype_int32 rawMeasurements[MAX_NUM_MEASUREMENTS];
	gtype_real64 volts[MAX_NUM_MEASUREMENTS];
	gtype_real64 calbMeasurements[MAX_NUM_MEASUREMENTS];
	gtype_int32 numMeasurements, i;


	GoIO_Sensor_SetMeasurementPeriod(hDevice, MeasurementPeriod, SKIP_TIMEOUT_MS_DEFAULT);//Example .040 => 40 milliseconds measurement period.
	GoIO_Sensor_SendCmdAndGetResponse(hDevice, SKIP_CMD_ID_START_MEASUREMENTS, NULL, 0, NULL, NULL, SKIP_TIMEOUT_MS_DEFAULT);

	
	numMeasurements = GoIO_Sensor_ReadRawMeasurements(hDevice, rawMeasurements, MAX_NUM_MEASUREMENTS);


	for (i = 0; i < numMeasurements; i++)
	{

		volts[i] = GoIO_Sensor_ConvertToVoltage(hDevice, rawMeasurements[i]);
		calbMeasurements[i] = GoIO_Sensor_CalibrateData(hDevice, volts[i]); //kPa

	}

	return calbMeasurements;
}*/

/*Method returns a the latest data value in the Sensor's buffer.*/
gtype_real64 getLatestData(GOIO_SENSOR_HANDLE hDevice) {
	gtype_int32 rawMeasurement = 0;
	gtype_real64 volts;
	gtype_real64 calbMeasurements;

	rawMeasurement = GoIO_Sensor_GetLatestRawMeasurement(hDevice);
	volts = GoIO_Sensor_ConvertToVoltage(hDevice, rawMeasurement);
	calbMeasurements = GoIO_Sensor_CalibrateData(hDevice, volts);

	return calbMeasurements;
}





bool initSensor() {
	char deviceName[GOIO_MAX_SIZE_DEVICE_NAME];
	gtype_int32 vendorId;		//USB vendor id
	gtype_int32 productId;		//USB product id
	GOIO_SENSOR_HANDLE hDevice;
	gtype_uint16 MajorVersion;
	gtype_uint16 MinorVersion;
	char units[20];
	char equationType = 0;

	gtype_int32 rawMeasurements[MAX_NUM_MEASUREMENTS];
	gtype_real64 volts[MAX_NUM_MEASUREMENTS];
	gtype_real64 calbMeasurements[MAX_NUM_MEASUREMENTS];
	gtype_int32 numMeasurements, i;
	gtype_real64 averageCalbMeasurement;


	GoIO_Init();
	GoIO_GetDLLVersion(&MajorVersion, &MinorVersion);
	printf("This app is linked to GoIO lib version %d.%d .\n", MajorVersion, MinorVersion);

	bool foundDevice = GetAvailableDeviceName(deviceName, GOIO_MAX_SIZE_DEVICE_NAME, &vendorId, &productId);

	if (foundDevice)
	{
		

		hDevice = openSensor(deviceName, vendorId, productId);
		hSensor = hDevice;
		printf("%p %p \n", hDevice, hSensor);
		GoIO_Sensor_SetMeasurementPeriod(hDevice, 0.030, SKIP_TIMEOUT_MS_DEFAULT);//40 milliseconds measurement period.
		GoIO_Sensor_SendCmdAndGetResponse(hDevice, SKIP_CMD_ID_START_MEASUREMENTS, NULL, 0, NULL, NULL, SKIP_TIMEOUT_MS_DEFAULT);
		Sleep(3000); //Wait 1 second.

		//test reading
		cout << "[BreathingPhaseSensor] Test Reading from Breathing Phase Sensor ... \n";

		numMeasurements = GoIO_Sensor_ReadRawMeasurements(hDevice, rawMeasurements, MAX_NUM_MEASUREMENTS);
		printf("[BreathingPhaseSensor] %d measurements received after about 1 second.\n", numMeasurements);
		averageCalbMeasurement = 0.0;
		for (i = 0; i < numMeasurements; i++)
		{
			volts[i] = GoIO_Sensor_ConvertToVoltage(hDevice, rawMeasurements[i]);
			calbMeasurements[i] = GoIO_Sensor_CalibrateData(hDevice, volts[i]);
			averageCalbMeasurement += calbMeasurements[i];
		}
		if (numMeasurements > 1)
			averageCalbMeasurement = averageCalbMeasurement / numMeasurements;

		GoIO_Sensor_DDSMem_GetCalibrationEquation(hDevice, &equationType);
		gtype_real32 a, b, c;
		unsigned char activeCalPage = 0;
		GoIO_Sensor_DDSMem_GetActiveCalPage(hDevice, &activeCalPage);
		GoIO_Sensor_DDSMem_GetCalPage(hDevice, activeCalPage, &a, &b, &c, units, sizeof(units));
		printf("[BreathingPhaseSensor] Average measurement = %8.3f %s .\n", averageCalbMeasurement, units);

	}
	if (foundDevice) return true;
		else return false;
}

void closeSensor() {
	if(hSensor)	GoIO_Sensor_Close(hSensor);
	GoIO_Uninit();

}
bool isSensorAvailable()
{

	if (hSensor) return true;
		else return false;
}
/*Uses function from sensor's SDK to pull data from the sensor's buffer*/
double getRawData()
{
	if(hSensor)	return GoIO_Sensor_GetLatestRawMeasurement(hSensor);
	else
	{
		cout << "ERROR : NO valid breathing phase sensor\n";
		return -1;
	}
	
}

/*Uses function from console application to pull data from the sensor's buffer*/
double getCalibratedData() {
	if (hSensor!=NULL)	return getLatestData(hSensor);
		else
	{
		cout << "ERROR : NO valid breathing phase sensor\n";
		return -1;
	}	
}
double getCalibratedDatafromMultiMeasures() {
	gtype_int32 numMeasurements, i;
	gtype_real64 averageCalbMeasurement;
	gtype_int32 rawMeasurements[MAX_NUM_MEASUREMENTS];
	gtype_real64 volts[MAX_NUM_MEASUREMENTS];
	gtype_real64 calbMeasurements[MAX_NUM_MEASUREMENTS];

	if (hSensor == NULL) {
		cout << "ERROR : NO valid breathing phase sensor\n";
		return -1;
	}

	numMeasurements = GoIO_Sensor_ReadRawMeasurements(hSensor, rawMeasurements, MAX_NUM_MEASUREMENTS);	
	averageCalbMeasurement = 0.0;

	if (numMeasurements < 1) return -1;

	for (i = 0; i < numMeasurements; i++)
	{
		volts[i] = GoIO_Sensor_ConvertToVoltage(hSensor, rawMeasurements[i]);
		calbMeasurements[i] = GoIO_Sensor_CalibrateData(hSensor, volts[i]);
		averageCalbMeasurement += calbMeasurements[i];
	}
	if (numMeasurements > 1)
		averageCalbMeasurement = averageCalbMeasurement / numMeasurements;

	return averageCalbMeasurement;
	
}