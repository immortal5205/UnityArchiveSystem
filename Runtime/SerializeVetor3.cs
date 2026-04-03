
using System;
using UnityEngine;
namespace NuoYan.Archive
{
    [Serializable]
    public struct SerializeVetor3
    {
        public float x;
        public float y;
        public float z;

        public SerializeVetor3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public SerializeVetor3(UnityEngine.Vector3 vector)
        {
            this.x = vector.x;
            this.y = vector.y;
            this.z = vector.z;
        }

        public UnityEngine.Vector3 ToVector3()
        {
            return new UnityEngine.Vector3(x, y, z);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }

        public static implicit operator UnityEngine.Vector3(SerializeVetor3 v)
        {
            return new UnityEngine.Vector3(v.x, v.y, v.z);
        }
        public static implicit operator SerializeVetor3(UnityEngine.Vector3 v)
        {
            return new SerializeVetor3(v.x, v.y, v.z);
        }
    }

    [Serializable]
    public struct SerializeVector2
    {
        public float x;
        public float y;

        public SerializeVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public SerializeVector2(UnityEngine.Vector2 vector)
        {
            this.x = vector.x;
            this.y = vector.y;
        }

        public UnityEngine.Vector2 ToVector2()
        {
            return new UnityEngine.Vector2(x, y);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }

        public static implicit operator UnityEngine.Vector2(SerializeVector2 v)
        {
            return new UnityEngine.Vector2(v.x, v.y);
        }
        public static implicit operator SerializeVector2(UnityEngine.Vector2 v)
        {
            return new SerializeVector2(v.x, v.y);
        }
    }

    [Serializable]
    public struct SerializeVector3Int
    {
        public int x;
        public int y;
        public int z;

        public SerializeVector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public SerializeVector3Int(UnityEngine.Vector3Int vector)
        {
            this.x = vector.x;
            this.y = vector.y;
            this.z = vector.z;
        }

        public UnityEngine.Vector3Int ToVector3Int()
        {
            return new UnityEngine.Vector3Int(x, y, z);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }

        public static implicit operator UnityEngine.Vector3Int(SerializeVector3Int v)
        {
            return new UnityEngine.Vector3Int(v.x, v.y, v.z);
        }
        public static implicit operator SerializeVector3Int(UnityEngine.Vector3Int v)
        {
            return new SerializeVector3Int(v.x, v.y, v.z);
        }
    }

    [Serializable]
    public struct SerializeVector2Int
    {
        public int x;
        public int y;

        public SerializeVector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public SerializeVector2Int(UnityEngine.Vector2Int vector)
        {
            this.x = vector.x;
            this.y = vector.y;
        }

        public UnityEngine.Vector2Int ToVector2Int()
        {
            return new UnityEngine.Vector2Int(x, y);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }

        public static implicit operator UnityEngine.Vector2Int(SerializeVector2Int v)
        {
            return new UnityEngine.Vector2Int(v.x, v.y);
        }
        public static implicit operator SerializeVector2Int(UnityEngine.Vector2Int v)
        {
            return new SerializeVector2Int(v.x, v.y);
        }
    }

    [Serializable]
    public struct SerializeQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SerializeQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public SerializeQuaternion(UnityEngine.Quaternion quaternion)
        {
            this.x = quaternion.x;
            this.y = quaternion.y;
            this.z = quaternion.z;
            this.w = quaternion.w;
        }

        public UnityEngine.Quaternion ToQuaternion()
        {
            return new UnityEngine.Quaternion(x, y, z, w);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z}, {w})";
        }

        public static implicit operator UnityEngine.Quaternion(SerializeQuaternion v)
        {
            return new UnityEngine.Quaternion(v.x, v.y, v.z, v.w);
        }
        public static implicit operator SerializeQuaternion(UnityEngine.Quaternion v)
        {
            return new SerializeQuaternion(v.x, v.y, v.z, v.w);
        }
    }
    [Serializable]
    public struct SerializeTransform
    {
        public SerializeVetor3 position;
        public SerializeQuaternion rotation;
        public SerializeVetor3 scale;

        public SerializeTransform(SerializeVetor3 position, SerializeQuaternion rotation, SerializeVetor3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public SerializeTransform(UnityEngine.Transform transform)
        {
            this.position = transform.position;
            this.rotation = transform.rotation;
            this.scale = transform.localScale;
        }


        public override string ToString()
        {
            return $"Position: {position}, Rotation: {rotation}, Scale: {scale}";
        }
    }
    [Serializable]
    public struct SerializeRectTransform
    {
        public SerializeTransform transform;
        public SerializeVector2 anchorMin;
        public SerializeVector2 anchorMax;
        public SerializeVector2 pivot;
        public SerializeVector2 sizeDelta;

        public SerializeRectTransform(SerializeTransform transform, SerializeVector2 anchorMin, SerializeVector2 anchorMax, SerializeVector2 pivot, SerializeVector2 sizeDelta)
        {
            this.transform = transform;
            this.anchorMin = anchorMin;
            this.anchorMax = anchorMax;
            this.pivot = pivot;
            this.sizeDelta = sizeDelta;
        }

        public SerializeRectTransform(UnityEngine.RectTransform rectTransform)
        {
            this.transform = new SerializeTransform(rectTransform);
            this.anchorMin = rectTransform.anchorMin;
            this.anchorMax = rectTransform.anchorMax;
            this.pivot = rectTransform.pivot;
            this.sizeDelta = rectTransform.sizeDelta;
        }

        public override string ToString()
        {
            return $"Transform: {transform}, AnchorMin: {anchorMin}, AnchorMax: {anchorMax}, Pivot: {pivot}, SizeDelta: {sizeDelta}";
        }
    }

    [Serializable]
    public struct SerializeColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public SerializeColor(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        public SerializeColor(UnityEngine.Color color)
        {
            this.r = color.r;
            this.g = color.g;
            this.b = color.b;
            this.a = color.a;
        }

        public UnityEngine.Color ToColor()
        {
            return new UnityEngine.Color(r, g, b, a);
        }

        public override string ToString()
        {
            return $"({r}, {g}, {b}, {a})";
        }

        public static implicit operator UnityEngine.Color(SerializeColor v)
        {
            return new UnityEngine.Color(v.r, v.g, v.b, v.a);
        }
        public static implicit operator SerializeColor(UnityEngine.Color v)
        {
            return new SerializeColor(v.r, v.g, v.b, v.a);
        }
    }

}