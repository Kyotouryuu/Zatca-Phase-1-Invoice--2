using System;
using System.Text;

namespace ZatcaDotNet.Services.Zatca
{
	public sealed class Tag
	{
		public int TagId { get; }
		public string Value { get; }

		public Tag(int tagId, string value)
		{
			TagId = tagId;
			Value = value ?? string.Empty;
		}

		public byte[] ToBytes()
		{
			var valueBytes = Encoding.UTF8.GetBytes(Value);

			if (TagId < 0 || TagId > 255)
			{
				throw new ArgumentOutOfRangeException(nameof(TagId), "Tag id must be between 0 and 255.");
			}
			if (valueBytes.Length > 255)
			{
				// Spec uses single byte length for Phase 1 payloads. Keep behavior aligned to PHP version.
				throw new ArgumentOutOfRangeException(nameof(Value), "Value length must be <= 255 bytes for ZATCA Phase 1.");
			}

			var buffer = new byte[2 + valueBytes.Length];
			buffer[0] = (byte)TagId;
			buffer[1] = (byte)valueBytes.Length;
			Buffer.BlockCopy(valueBytes, 0, buffer, 2, valueBytes.Length);
			return buffer;
		}
	}
}


