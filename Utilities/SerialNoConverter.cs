using System.Text;

namespace dotnet_core_web_client.Utilities
{
	/// <summary>
	/// background: when a new device is first powered up, it will fetch a serial number from the photo.iguardpayroll.com,
	/// and jacky will return a sequencially increasing number, and the device will convert it to uint and saved it for
	/// later use, using the same algorithm below. So when the device sends an accessLog to iGuardPayroll, it only includes
	/// the string serial number, but will send the uint to the photo server. That's why I need to convert the string to uint
	/// when trying to get the pictures of a particular accessLog from the photo server. (240403)
	/// - max serial number: xx00-1048-575x
	/// </summary>
	public class SerialNoConverter
	{
		// both of these two functions are written by Jacky with slight modification (refer to whatsapp) (240328)
		public static uint StringToUint(string sn1)
		{
			uint rc;
			uint cs;
			uint prefix;
			uint prefix2;

			string sn = sn1.Replace("-", "");
			cs = sn[sn.Length - 1];                 // last digit is checksum
			prefix = sn[0];                         // first digit prefix
			prefix2 = sn[1];
			sn = sn.Substring(2, sn.Length - 3);    // remove first & 2nd and last digit

			rc = uint.Parse(sn);

			rc = (rc << 4) & 0xFFFFFF;              // shift one digit and remove MS digit
			rc |= cs - '0';
			rc |= (prefix - '0') << 28;
			rc |= (prefix2 - '0') << 24;

			return rc;
		}

		public static string UintToString(uint hsn)
		{
			string sn;

			bool isWrongConversionInEarlyiGuardExpress540(uint x) => (x & 0xFF000000) == 0x39000000;
			bool isLM520(uint x) => x < 0x60000000;

			if (isWrongConversionInEarlyiGuardExpress540(hsn))
			{
				// for wrong conversion used in early iGuardExpress540 (240410)
				// - refer to WebSocketManager.cs -> OnDeviceConnectedAsync() for more info (240410)
				uint u1 = hsn - 0x80000000;
				uint u3 = hsn & 0x0f;
				uint u2 = (u1 >> 4) | 0x30000000;
				sn = new StringBuilder("7").Append(u2.ToString("D10")).Append(u3.ToString()).Insert(4, "-").Insert(9, "-").ToString();
			}
			else if (isLM520(hsn))
			{
				// Most significant 4 bits < 7 is serial no. for LM520
				uint sn1 = hsn >> 16;
				uint sn2 = hsn & 0xFFFF;
				int yearcode = (hsn < 0x50000000) ? 9940 : 2003;
				sn = $"VK-{yearcode}-{sn1:X4}-{sn2:X4}";
			}
			else
			{
				uint p = (hsn & 0xFF000000) >> 24;  // Prefix
				uint n = (hsn & 0x00FFFFFF) >> 4;   // SN
				uint cs = hsn & 0xF;                // Checksum
				sn = new StringBuilder(p.ToString("X2")).Append(n.ToString("D9")).Append(cs).Insert(4, "-").Insert(9, "-").ToString();
			}

			return sn;
		}

		public static bool IsValidSerialNo(string sn)
		{
			int cs = 0;
			int n = 2;
			int dashes = 0;
			int last = 0;
			int len = sn.Length;
			int start = 2;

			if (len < 12)
			{
				return false;
			}

			cs = 7;

			for (int i = start; i < len; i++)
			{
				char ch = sn[i];

				if (char.IsDigit(ch))
				{
					last = ch - '0';
					cs += last;
					n++;
				}
				else if (ch != '-' || ++dashes > 2)
				{
					return false;
				}
			}

			if (n != 12 || dashes != 2)
			{
				return false;
			}

			cs = (cs - last) % 10;

			return (cs == last);
		}
	}
}
