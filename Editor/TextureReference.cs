﻿using System;

namespace UnityToCustomEngineExporter.Editor
{
    public class TextureScaleReference: TextureReference, IEquatable<TextureScaleReference>
    {
        public bool Equals(TextureScaleReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && _scale.Equals(other._scale);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TextureScaleReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ _scale.GetHashCode();
            }
        }

        public static bool operator ==(TextureScaleReference left, TextureScaleReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TextureScaleReference left, TextureScaleReference right)
        {
            return !Equals(left, right);
        }

        private readonly float _scale;

        public TextureScaleReference(TextureSemantic semantic, float scale):base(semantic)
        {
            _scale = scale;
        }
    }

    public class TextureReference : IEquatable<TextureReference>
    {
        public bool Equals(TextureReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Semantic == other.Semantic;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TextureReference) obj);
        }

        public override int GetHashCode()
        {
            return (int) Semantic;
        }

        public static bool operator ==(TextureReference left, TextureReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TextureReference left, TextureReference right)
        {
            return !Equals(left, right);
        }

        public TextureSemantic Semantic;

        public TextureReference(TextureSemantic semantic)
        {
            Semantic = semantic;
        }
    }
}