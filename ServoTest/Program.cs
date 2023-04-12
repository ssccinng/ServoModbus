// See https://aka.ms/new-console-template for more information
using ServoModbus;
ServoClient servo = new ServoClient("COM1");

await servo.SetDo(0, true);