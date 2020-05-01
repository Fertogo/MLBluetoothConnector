using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class BluetoothConnector : MonoBehaviour
{

    private byte[] BLEBroadcast = { 0x01, 0x00 };
    private byte[] BLERead = { 0x02, 0x00 };
    private byte[] BLEWriteWithoutResponse = { 0x04, 0x00 };
    private byte[] BLEWrite = { 0x08, 0x00 };
    private byte[] BLENotify = { 0x10, 0x00 }; 

    private string KEYBOARD_ADDRESS = "D7:91:9F:52:2E:02";
    private string SERVICE_UUID = "03b80e5a-ede8-4b33-a751-6ce34ec4c700";

    float timePassed = 0;
    private MLBluetoothLE.Characteristic characteristic;
    private MLBluetoothLE.Device device; 


    // Start is called before the first frame update
    void Start()
    {
        print("BluetoothConnector Started: V1"); 


        MLBluetoothLE.Start();
        MLBluetoothLE.StartScan();

        MLBluetoothLE.OnScanResult += onScanResult;
        MLBluetoothLE.OnBluetoothConnectionStateChanged += onBluetoothConnectionStateChanged;
        MLBluetoothLE.OnBondStateChanged += onBondStateChanged;
        MLBluetoothLE.OnBluetoothCharacteristicRead += onCharacteristicRead;
        MLBluetoothLE.OnBluetoothGattRemoteCharacteristicChanged += onCharacteristicChanged; 
        MLBluetoothLE.OnBluetoothServicesDiscovered += onBLEServicesDiscovered;
        MLBluetoothLE.OnBluetoothGattDescriptorWrite += onBluetoothGattDescriptionWrite;
        MLBluetoothLE.OnBluetoothCharacteristicWrite += onCharacteristicWrite; 
    }


    void onScanResult(MLBluetoothLE.Device device)
    {
       // print("Scan Result");
       // print(device.Address);


        if (device.Address == KEYBOARD_ADDRESS) {
            print("Keyboard found!");
            print(device.Name);
            print(device.DeviceType.ToString());

            this.device = device; 

            MLBluetoothLE.StopScan();

            print("Attempting to connect"); 
            MLBluetoothLE.GattConnect(device.Address);
        }


    }

    void onBluetoothConnectionStateChanged(MLBluetoothLE.Status status, MLBluetoothLE.GattConnectionState state)
    {
        print(status.ToString());
        print(state.ToString());

        if(state.Equals(MLBluetoothLE.GattConnectionState.Connected)) {
            print("Connected to keyboard");
             MLBluetoothLE.DiscoverServices();
           // MLBluetoothLE.CreateBond(device.Address); 
        } else {
            print("Disconnected from keyboard"); 
        }
        
    }

    void onBondStateChanged(MLBluetoothLE.Device device, MLBluetoothLE.BondState state)
    {
        print("onBondStateChanged"); 
        
        if (state.Equals(MLBluetoothLE.BondState.Bonded)) {
            print("Device bonded!");
            MLBluetoothLE.DiscoverServices();
        }
    }



    void onBLEServicesDiscovered(MLBluetoothLE.Status status)
    {
        print("onBLEServicesDiscovered: " + status.ToString());
        // You can now read and write to GATT characteristics and descriptors
        // or set notification for when a GATT characteristic changes.
        // Each time you start an operation on the GATT server, you must wait until 
        // the corresponding callback is called before you can start another operation.
        // The callbacks include the operation status, which you can use to determine 
        // next steps on the operation.
        MLBluetoothLE.Service[] services;

        MLBluetoothLE.GetServiceRecord(out services);
        print("Found " + services.Length + " services");

        printBLEState(); 

        foreach (MLBluetoothLE.Service service in services) {
            // print(service.Uuid);
            if (service.Uuid.Equals(SERVICE_UUID)) {
                print("---------Found Service!!!!!!!!!");

                //foreach (MLBluetoothLE.Characteristic characteristic in service.Characteristics) {'


                // This characteristic is neither readable or writable. a client can obtain its value only through notifications sent by the server.
                this.characteristic = service.Characteristics[0];
                print("Enabling notifications for: " + characteristic.Uuid);
          

                printCharacteristicInfo();


                characteristic.WriteType = MLBluetoothLE.WriteType.Default; 


               MLBluetoothLE.EnableCharacteristicNotification(characteristic, true);


                // Every time a client wants to enable notifications or indications for a particular characteristic that supports them, it simply uses a Write Request ATT packet to set the corresponding bit to 1
                descriptor = characteristic.Descriptors[0];
                //descriptor.Buffer = new byte[] { 0x01 | 0x02 | 0x04 | 0x10, 0x00 };  
                //descriptor.Buffer = new byte[] { 0x11, 0x11 };

                descriptor.Buffer = new byte[] { 0x01, 0x00 }; 

                printDescriptorInfo(descriptor);

                MLBluetoothLE.WriteDescriptor(descriptor);
                MLBluetoothLE.EnableCharacteristicNotification(characteristic, true);


                characteristic.Descriptors[0] = descriptor;

                MLBluetoothLE.WriteCharacteristic(characteristic); 

            


                //MLBluetoothLE.EnableCharacteristicNotification(characteristic, true);

            }

        }


    }

    void onBluetoothGattDescriptionWrite(MLBluetoothLE.Descriptor descriptor, MLBluetoothLE.Status status)
    {
        print("------------onBluetoothGattDescriptorWrite");
        // MLBluetoothLE.EnableCharacteristicNotification(characteristic, true);
        // MLBluetoothLE.OnBluetoothGattRemoteCharacteristicChanged += onCharacteristicChanged;
        MLBluetoothLE.EnableCharacteristicNotification(characteristic, true);

        printDescriptorInfo(descriptor);
        printCharacteristicInfo();

        MLBluetoothLE.ReadCharacteristic(ref characteristic); 



    }

    void onCharacteristicWrite(MLBluetoothLE.Characteristic characteristic, MLBluetoothLE.Status status)
    {
        print("--------------------------------------------------onCharacteristicWrite");
        this.characteristic = characteristic;
        printCharacteristicInfo(); 

    }

    void onCharacteristicRead(MLBluetoothLE.Characteristic characteristic, MLBluetoothLE.Status status)
    {
        print("**********************************--------------------------------------------------onCharacteristicRead");
        print(string.Join(",", characteristic.Buffer));
    }

    void onCharacteristicChanged(MLBluetoothLE.Characteristic characteristic)
    {
        print("**************************************-------------------------------------------------onCharaceristicChanged");
        print(string.Join(",", characteristic.Buffer));
    }



    private void printBLEState()
    {
        MLBluetoothLE.AdapterState adapterState;
        MLBluetoothLE.GetAdapterState(out adapterState);
        print("Adapter state: " + adapterState.ToString());

    }

    private void printCharacteristicInfo()
    {
        // Problem: Permissions are NONE while connected or bonded 
        print("Characteristinc UUID: " + characteristic.Uuid);
        print("  Permissions: " + characteristic.Permissions.ToString());
        print("  Properties: " + characteristic.Properties.ToString());
        print("  Buffer: " + string.Join(", ", characteristic.Buffer));
        foreach (MLBluetoothLE.Descriptor descriptor in characteristic.Descriptors) {
            printDescriptorInfo(descriptor); 
        }
    }

    private void printDescriptorInfo(MLBluetoothLE.Descriptor descriptor)
    {
        print("Descriptor Info. UUID: " + descriptor.Uuid);
        print("Descritor Buffer Size: " + descriptor.Buffer.Length);
        print("Descritor Buffer: " + string.Join(", ", descriptor.Buffer));
    }

    private void print(string message)
    {
        Debug.LogWarning("BluetoothConnector:_" + message);
    }

    // Update is called once per frame
    private MLBluetoothLE.Descriptor descriptor; 
    private byte attempt = 0x00; 
    void Update()
    {

        timePassed += Time.deltaTime; 

        if (timePassed > 2) {
            timePassed = 0;
          //  attempt++; 
        //    print("Going to write new value");
        //    descriptor.Buffer = new byte[] { attempt, 0x00 };
        //    MLBluetoothLE.WriteDescriptor(descriptor);

        }
    }
}
