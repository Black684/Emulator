using System.Linq;
using global::NModbus.Device;
using global::NModbus.Message;
using global::NModbus;
using System;
using Sensors;

namespace Emulator
{
    public class WriteMultipleRegistersService : ModbusFunctionServiceBase<WriteMultipleRegistersRequest>
    {
        public WriteMultipleRegistersService() : base(ModbusFunctionCodes.WriteMultipleRegisters)
        {
        }

        public override IModbusMessage CreateRequest(byte[] frame)
        {
            return CreateModbusMessage<WriteMultipleRegistersRequest>(frame);
        }

        public override int GetRtuRequestBytesToRead(byte[] frameStart)
        {
            return frameStart[6] + 2;
        }

        public override int GetRtuResponseBytesToRead(byte[] frameStart)
        {
            return 4;
        }

        protected override IModbusMessage Handle(WriteMultipleRegistersRequest request, ISlaveDataStore dataStore)
        {
            if ((request.StartAddress + request.NumberOfPoints - 1) > ReadHoldingRegistersService.MaxEndAddress)
            {
                throw new InvalidModbusRequestException(SlaveExceptionCodes.IllegalDataAddress);
            }

            ushort[] registers = request.Data.ToArray();

            var dimensionRegisterAddress = 0x01;
            var startAddress = request.StartAddress;
            var endAddress = request.StartAddress + request.NumberOfPoints - 1;

            if (dimensionRegisterAddress >= startAddress && dimensionRegisterAddress <= endAddress)
            {
                var index = dimensionRegisterAddress - startAddress;
                try
                {
                    var a = DimensionConverter.Default.Convert(ByteManipulater.GetLSB(registers[index]));
                    dataStore.HoldingRegisters.WritePoints(request.StartAddress, registers);
                }
                catch 
                {
                    throw new InvalidModbusRequestException(SlaveExceptionCodes.IllegalDataValue);
                }
            }
            return new WriteMultipleRegistersResponse(request.SlaveAddress, request.StartAddress, request.NumberOfPoints);
        }
    }
}

