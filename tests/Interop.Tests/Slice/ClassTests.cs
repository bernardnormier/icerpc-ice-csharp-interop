// Copyright (c) ZeroC, Inc.

using Ice;
using NUnit.Framework;
using System.Buffers;
using System.IO.Pipelines;
using ZeroC.Slice;

namespace Interop.Tests.Slice;

[Parallelizable(scope: ParallelScope.All)]
public class ClassTests
{
    /// <summary>Encodes a class with the .ice-generated code and decodes this class with the .slice-generated code.
    /// </summary>
    [Test]
    public void Ice_class_to_slice_class([Values] bool slicedFormat)
    {
        var value = new Bicycle("xyz", hasBasket: true);

        BicycleTwin decodedValue = IceToSlice(
            slicedFormat,
            outputStream => outputStream.writeValue(value),
            (ref SliceDecoder decoder) => decoder.DecodeClass<BicycleTwin>());

        Assert.That(decodedValue.Name, Is.EqualTo(value.name));
        Assert.That(decodedValue.HasBasket, Is.EqualTo(value.hasBasket));
    }

    /// <summary>Encodes a class with the .slice-generated code and decodes this class with the .ice-generated code.
    /// </summary>
    [Test]
    public void Slice_class_to_ice_class([Values] bool slicedFormat)
    {
        var value = new BicycleTwin("xyz", hasBasket: true);

        Bicycle decodedValue = SliceToIce(
            slicedFormat,
            (ref SliceEncoder encoder) => encoder.EncodeClass(value),
            inputStream =>
            {
                var bicycle = new Bicycle();
                inputStream.readValue<Bicycle>(v => bicycle = v);
                return bicycle;
            });

        Assert.That(decodedValue.name, Is.EqualTo(value.Name));
        Assert.That(decodedValue.hasBasket, Is.EqualTo(value.HasBasket));
    }

    /// <summary>Encodes a fairly complex class graph with the .ice-generated code and decodes this graph with the
    /// .slice-generated code. This verifies the "reference semantics" of class instances.</summary>
    [Test]
    public void Ice_class_graph_to_slice_class_graph([Values] bool slicedFormat)
    {
        var truck = new Truck();

        var bicycle1 = new Bicycle("b1", hasBasket: true);
        var bicycle2 = new Bicycle("b2", hasBasket: false);
        var bicycle3 = new Bicycle("b3", hasBasket: true);

        // Here the truck's cargo includes itself.
        truck.cargo = new Vehicle[] { bicycle1, bicycle2, bicycle3, bicycle1, truck };

        TruckTwin truckTwin = IceToSlice(
            slicedFormat,
            outputStream => outputStream.writeValue(truck),
            (ref SliceDecoder decoder) => decoder.DecodeClass<TruckTwin>());

        Assert.That(truckTwin.Cargo, Has.Count.EqualTo(truck.cargo.Length));
        Assert.That(truckTwin.Cargo[0], Is.InstanceOf<BicycleTwin>());
        Assert.That(truckTwin.Cargo[1], Is.InstanceOf<BicycleTwin>());
        Assert.That(truckTwin.Cargo[2], Is.InstanceOf<BicycleTwin>());
        Assert.That(truckTwin.Cargo[3], Is.SameAs(truckTwin.Cargo[0]));
        Assert.That(truckTwin.Cargo[4], Is.SameAs(truckTwin));
    }

    /// <summary>Encodes a fairly complex class graph with the .slice-generated code and decodes this graph with the
    /// .ice-generated code. This verifies the "reference semantics" of class instances.</summary>
    [Test]
    public void Slice_class_graph_to_ice_class_Gra([Values] bool slicedFormat)
    {
        var truckTwin = new TruckTwin("carrier", new List<VehicleTwin>());

        var bicycleTwin1 = new BicycleTwin("b1", hasBasket: true);
        truckTwin.Cargo.Add(bicycleTwin1);
        truckTwin.Cargo.Add(new BicycleTwin("b2", hasBasket: false));
        truckTwin.Cargo.Add(new BicycleTwin("b3", hasBasket: true));
        truckTwin.Cargo.Add(bicycleTwin1);
        // The truck's cargo includes itself:
        truckTwin.Cargo.Add(truckTwin);

        Truck truck = SliceToIce(
            slicedFormat,
            (ref SliceEncoder encoder) => encoder.EncodeClass(truckTwin),
            inputStream =>
            {
                var truck = new Truck();
                inputStream.readValue<Truck>(v => truck = v);
                return truck;
            });

        Assert.That(truck.cargo, Has.Length.EqualTo(truckTwin.Cargo.Count));
        Assert.That(truck.cargo[0], Is.InstanceOf<Bicycle>());
        Assert.That(truck.cargo[1], Is.InstanceOf<Bicycle>());
        Assert.That(truck.cargo[2], Is.InstanceOf<Bicycle>());
        Assert.That(truck.cargo[3], Is.SameAs(truck.cargo[0]));
        Assert.That(truck.cargo[4], Is.SameAs(truck));
    }

    /// <summary>Verifies that a class encoded with the .ice generated code in sliced format can be successfully sliced
    /// when decoded with the .slice generated code.</summary>
    [Test]
    public void Ice_class_to_slice_class_with_slicing()
    {
        var truck = new Truck();

        var bicycle1 = new MountainBike("mtb", hasBasket: false, suspensionType: "full suspension");
        var bicycle2 = new Bicycle("plain", hasBasket: true);

        truck.cargo = new Vehicle[] { bicycle1, bicycle2, bicycle1 };

        TruckTwin truckTwin = IceToSlice(
            slicedFormat: true,
            outputStream => outputStream.writeValue(truck),
            (ref SliceDecoder decoder) => decoder.DecodeClass<TruckTwin>());

        Assert.That(truckTwin.Cargo, Has.Count.EqualTo(truck.cargo.Length));
        Assert.That(truckTwin.Cargo[0], Is.InstanceOf<BicycleTwin>());
        Assert.That(truckTwin.Cargo[1], Is.InstanceOf<BicycleTwin>());
        Assert.That(truckTwin.Cargo[2], Is.SameAs(truckTwin.Cargo[0]));
    }

    /// <summary>Verifies that a class encoded with the .slice generated code in sliced format can be successfully
    /// sliced when decoded with the .ice generated code.</summary>
    [Test]
    public void Slice_class_to_ice_class_with_slicing()
    {
        var truckTwin = new TruckTwin("carrier", new List<VehicleTwin>());

        var bicycleTwin1 = new RacingBicycle("b1", hasBasket: false, maxSpeed: 60.0);
        truckTwin.Cargo.Add(bicycleTwin1);
        truckTwin.Cargo.Add(new BicycleTwin("b2", hasBasket: false));
        truckTwin.Cargo.Add(bicycleTwin1);

        Truck truck = SliceToIce(
            slicedFormat: true,
            (ref SliceEncoder encoder) => encoder.EncodeClass(truckTwin),
            inputStream =>
            {
                var truck = new Truck();
                inputStream.readValue<Truck>(v => truck = v);
                return truck;
            });

        Assert.That(truck.cargo, Has.Length.EqualTo(truckTwin.Cargo.Count));
        Assert.That(truck.cargo[0], Is.InstanceOf<Bicycle>());
        Assert.That(truck.cargo[1], Is.InstanceOf<Bicycle>());
        Assert.That(truck.cargo[2], Is.SameAs(truck.cargo[0]));
    }

    /// <summary>Verifies that a class encoded with the .ice generated code in sliced format is preserved when encoded
    /// and then re-encoded with the .slice generated code.</summary>
    [Test]
    public void Ice_class_preserved_by_slice()
    {
        var truck = new Truck();

        var bicycle1 = new MountainBike("mtb", hasBasket: false, suspensionType: "full suspension");
        var bicycle2 = new Bicycle("plain", hasBasket: true);

        truck.cargo = new Vehicle[] { bicycle1, bicycle2, bicycle1 };

        TruckTwin truckTwin = IceToSlice(
            slicedFormat: true,
            outputStream => outputStream.writeValue(truck),
            (ref SliceDecoder decoder) => decoder.DecodeClass<TruckTwin>());

        Truck newTruck = SliceToIce(
            slicedFormat: true,
            (ref SliceEncoder encoder) => encoder.EncodeClass(truckTwin),
            inputStream =>
            {
                var truck = new Truck();
                inputStream.readValue<Truck>(v => truck = v);
                return truck;
            });

        Assert.That(newTruck.cargo, Has.Length.EqualTo(truck.cargo.Length));
        Assert.That(newTruck.cargo[0], Is.InstanceOf<MountainBike>());
        Assert.That(newTruck.cargo[1], Is.InstanceOf<Bicycle>());
        Assert.That(newTruck.cargo[2], Is.SameAs(newTruck.cargo[0]));
        Assert.That(((MountainBike)newTruck.cargo[0]).suspensionType, Is.EqualTo(bicycle1.suspensionType));
    }

    /// <summary>Verifies that a class encoded with the .ice generated code in sliced format is preserved when encoded
    /// and then re-encoded with the .ice generated code.</summary>
    [Test]
    public void Slice_class_preserved_by_ice()
    {
        var truckTwin = new TruckTwin("carrier", new List<VehicleTwin>());

        var bicycleTwin1 = new RacingBicycle("b1", hasBasket: false, maxSpeed: 60.0);
        truckTwin.Cargo.Add(bicycleTwin1);
        truckTwin.Cargo.Add(new BicycleTwin("b2", hasBasket: false));
        truckTwin.Cargo.Add(bicycleTwin1);

        Truck truck = SliceToIce(
            slicedFormat: true,
            (ref SliceEncoder encoder) => encoder.EncodeClass(truckTwin),
            inputStream =>
            {
                var truck = new Truck();
                inputStream.readValue<Truck>(v => truck = v);
                return truck;
            });

        TruckTwin newTruckTwin = IceToSlice(
            slicedFormat: true,
            outputStream => outputStream.writeValue(truck),
            (ref SliceDecoder decoder) => decoder.DecodeClass<TruckTwin>());

        Assert.That(newTruckTwin.Cargo, Has.Count.EqualTo(truckTwin.Cargo.Count));
        Assert.That(newTruckTwin.Cargo[0], Is.InstanceOf<RacingBicycle>());
        Assert.That(newTruckTwin.Cargo[1], Is.InstanceOf<BicycleTwin>());
        Assert.That(newTruckTwin.Cargo[2], Is.SameAs(newTruckTwin.Cargo[0]));
        Assert.That(((RacingBicycle)newTruckTwin.Cargo[0]).MaxSpeed, Is.EqualTo(bicycleTwin1.MaxSpeed));
    }

    private static T IceToSlice<T>(bool slicedFormat, Action<OutputStream> encodeAction, DecodeFunc<T> decodeFunc)
    {
        using Communicator communicator = Util.initialize();
        var outputStream = new OutputStream(communicator);
        outputStream.startEncapsulation(
            Util.Encoding_1_1,
            slicedFormat ? FormatType.SlicedFormat : FormatType.CompactFormat);
        encodeAction(outputStream);
        outputStream.endEncapsulation();
        byte[] buffer = outputStream.finished();

        var decoder = new SliceDecoder(
            buffer,
            SliceEncoding.Slice1,
            activator: IActivator.FromAssembly(typeof(ClassTests).Assembly));

        // Skip encapsulation header
        decoder.Skip(6);
        T result = decodeFunc(ref decoder);
        Assert.That(decoder.Consumed, Is.EqualTo(buffer.Length));
        return result;
    }

    private static T SliceToIce<T>(bool slicedFormat, EncodeAction encodeAction, Func<InputStream, T> decodeFunc)
    {
        var pipe = new Pipe();
        var encoder = new SliceEncoder(
            pipe.Writer,
            SliceEncoding.Slice1,
            classFormat: slicedFormat ? ClassFormat.Sliced : ClassFormat.Compact);

        // encapsulation header
        Span<byte> sizePlaceholder = encoder.GetPlaceholderSpan(4);
        encoder.EncodeUInt8(1);
        encoder.EncodeUInt8(1);
        encodeAction(ref encoder);
        int size = encoder.EncodedByteCount;
        SliceEncoder.EncodeInt32(size, sizePlaceholder);

        pipe.Writer.Complete();
        pipe.Reader.TryRead(out ReadResult readResult);

        using Communicator communicator = Util.initialize();
        var inputStream = new InputStream(communicator, readResult.Buffer.ToArray());
        pipe.Reader.Complete();

        _ = inputStream.startEncapsulation();
        T result = decodeFunc(inputStream);
        inputStream.endEncapsulation();

        return result;
    }
}
