using System;
using System.Xml.Serialization;
using UnityEngine;

[Serializable]
public struct SerializableVector2
{
    public static SerializableVector2 zero => new SerializableVector2(0, 0);
    public static SerializableVector2 one => new SerializableVector2(1, 1);

    [XmlAttribute]
    public float x { get; set; }

    [XmlAttribute]
    public float y { get; set; }

    public SerializableVector2(float x, float y) => (this.x, this.y) = (x, y);

    public override string ToString() => $"(x: {x}, y: {y})";

    public static implicit operator Vector2(SerializableVector2 v) => new Vector3(v.x, v.y);

    public static implicit operator Vector3(SerializableVector2 v) => new Vector3(v.x, v.y);

    public static implicit operator SerializableVector2(Vector2 v) =>
        new SerializableVector2(v.x, v.y);

    public static implicit operator SerializableVector2(Vector3 v) =>
        new SerializableVector2(v.x, v.y);

    public static SerializableVector2 operator +(SerializableVector2 a, SerializableVector2 b) =>
        new SerializableVector2(a.x + b.x, a.y + b.y);

    public static SerializableVector2 operator -(SerializableVector2 a, SerializableVector2 b) =>
        new SerializableVector2(a.x - b.x, a.y - b.y);

    public static SerializableVector2 operator *(SerializableVector2 a, float d) =>
        new SerializableVector2(a.x * d, a.y * d);

    public static SerializableVector2 operator /(SerializableVector2 a, float d) =>
        new SerializableVector2(a.x / d, a.y / d);
}

[Serializable]
public struct SerializableVector3
{
    [XmlAttribute]
    public float x { get; set; }

    [XmlAttribute]
    public float y { get; set; }

    [XmlAttribute]
    public float z { get; set; }

    public SerializableVector3(float x, float y, float z) => (this.x, this.y, this.z) = (x, y, z);

    public override string ToString() => $"(x: {x}, y: {y}, z: {z})";

    public static implicit operator Vector3(SerializableVector3 v) => new Vector3(v.x, v.y, v.z);

    public static implicit operator SerializableVector3(Vector3 v) =>
        new SerializableVector3(v.x, v.y, v.z);

    public static SerializableVector3 operator +(SerializableVector3 a, SerializableVector3 b) =>
        new SerializableVector3(a.x + b.x, a.y + b.y, a.z + b.z);

    public static SerializableVector3 operator -(SerializableVector3 a, SerializableVector3 b) =>
        new SerializableVector3(a.x - b.x, a.y - b.y, a.z - b.z);

    public static SerializableVector3 operator *(SerializableVector3 a, float d) =>
        new SerializableVector3(a.x * d, a.y * d, a.z * d);

    public static SerializableVector3 operator /(SerializableVector3 a, float d) =>
        new SerializableVector3(a.x / d, a.y / d, a.z / d);
}

[Serializable]
public struct SerializableVector4
{
    [XmlAttribute]
    public float x { get; set; }

    [XmlAttribute]
    public float y { get; set; }

    [XmlAttribute]
    public float z { get; set; }

    [XmlAttribute]
    public float w { get; set; }

    public SerializableVector4(float x, float y, float z, float w) =>
        (this.x, this.y, this.z, this.w) = (x, y, z, w);

    public override string ToString() => $"(x: {x}, y: {y}, z: {z}, w: {w})";

    public static implicit operator Vector4(SerializableVector4 v) =>
        new Vector4(v.x, v.y, v.z, v.w);

    public static implicit operator SerializableVector4(Vector4 v) =>
        new SerializableVector4(v.x, v.y, v.z, v.w);
}

[Serializable]
public struct SerializableQuaternion
{
    [XmlAttribute]
    public float x { get; set; }

    [XmlAttribute]
    public float y { get; set; }

    [XmlAttribute]
    public float z { get; set; }

    [XmlAttribute]
    public float w { get; set; }

    public SerializableQuaternion(float x, float y, float z, float w) =>
        (this.x, this.y, this.z, this.w) = (x, y, z, w);

    public override string ToString() => $"(x: {x}, y: {y}, z: {z}, w: {w})";

    public static implicit operator Quaternion(SerializableQuaternion q) =>
        new Quaternion(q.x, q.y, q.z, q.w);

    public static implicit operator SerializableQuaternion(Quaternion q) =>
        new SerializableQuaternion(q.x, q.y, q.z, q.w);
}
