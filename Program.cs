using System;
using System.Collections.Generic;

class Program
{
    static bool IsEmpty(string text)
    {
        return string.IsNullOrWhiteSpace(text);
    }

    static bool IsCorrectNetworkAddress(string address)
    {
        string[] octets = address.Split('.');
        if (octets.Length != 4)
        {
            return false;
        }

        foreach (string octet in octets)
        {
            if (!int.TryParse(octet, out int value) || value < 0 || value > 255)
            {
                return false;
            }
        }

        return true;
    }

    static bool IsCorrectEndpointNumbersPerNetwork(string numbers)
    {
        string[] endpoints = numbers.Split(',');
        foreach (string endpoint in endpoints)
        {
            if (!int.TryParse(endpoint, out int value) || value <= 0)
            {
                return false;
            }
        }

        return true;
    }

    static bool IsCorrectPrefix(string prefix)
    {
        return int.TryParse(prefix, out int value) && value >= 0 && value <= 32;
    }

    static int PowerBitLength(int x)
    {
        return (int)Math.Pow(2, (int)Math.Ceiling(Math.Log(x + 1, 2)));
    }

    static string GetMaskFromPrefix(int prefix)
    {
        uint mask = ~((1u << (32 - prefix)) - 1);
        byte[] maskBytes = BitConverter.GetBytes(mask);
        Array.Reverse(maskBytes);  // Revertir los bytes para que estén en el orden correcto
        return new System.Net.IPAddress(maskBytes).ToString();
    }

    static string Get32BitFormat(string ipAddress)
    {
        string format32Bit = "";
        string[] octets = ipAddress.Split('.');
        foreach (string octet in octets)
        {
            format32Bit += Convert.ToString(int.Parse(octet), 2).PadLeft(8, '0');
        }

        return format32Bit;
    }

    static string GetIpFrom32BitFormat(string format32Bit)
    {
        string ipDec = "";
        for (int i = 0; i < format32Bit.Length; i += 8)
        {
            ipDec += Convert.ToInt32(format32Bit.Substring(i, 8), 2) + ".";
        }

        return ipDec.TrimEnd('.');
    }

    static string GetFirstAddressableIp(string networkIp)
    {
        string firstAddressableIpBin32Bit = Convert.ToString(
            Convert.ToInt32(Get32BitFormat(networkIp), 2) + 1, 2).PadLeft(32, '0');
        return GetIpFrom32BitFormat(firstAddressableIpBin32Bit);
    }

    static string GetLastAddressableIp(string networkIp, int subnetSize)
    {
        string lastAddressableIpBin32Bit = Convert.ToString(
            Convert.ToInt32(Get32BitFormat(networkIp), 2) + subnetSize - 2, 2).PadLeft(32, '0');
        return GetIpFrom32BitFormat(lastAddressableIpBin32Bit);
    }

    static string GetBroadcastIp(string networkIp, int subnetSize)
    {
        string broadcastIpBin32Bit = Convert.ToString(
            Convert.ToInt32(Get32BitFormat(networkIp), 2) + subnetSize - 1, 2).PadLeft(32, '0');
        return GetIpFrom32BitFormat(broadcastIpBin32Bit);
    }

    static string GetNextNetworkIp(string networkIp, int subnetSize)
    {
        string nextNetworkIpBin32Bit = Convert.ToString(
            Convert.ToInt32(Get32BitFormat(networkIp), 2) + subnetSize, 2).PadLeft(32, '0');
        return GetIpFrom32BitFormat(nextNetworkIpBin32Bit);
    }

    static List<Dictionary<string, object>> CalculateVlsm(string networkIp, string endpointNumbersPerNetwork, string prefix)
    {
        List<Dictionary<string, object>> subnets = new List<Dictionary<string, object>>();
        string[] networkHosts = endpointNumbersPerNetwork.Split(',');
        List<int> lengthOfSubnets = new List<int>();

        foreach (string hosts in networkHosts)
        {
            if (int.TryParse(hosts, out int value) && value > 0)
            {
                value += 2;
                lengthOfSubnets.Add(PowerBitLength(value));
            }
        }

        lengthOfSubnets.Sort((a, b) => b.CompareTo(a));
        int sumAllHosts = lengthOfSubnets.Sum();

        if (IsEmpty(prefix))
        {
            int firstOctet = int.Parse(networkIp.Split('.')[0]);

            if (firstOctet >= 1 && firstOctet < 128)
            {
                if (sumAllHosts <= Math.Pow(2, 24))
                {
                    InjectDataToDictionary(networkIp, lengthOfSubnets, subnets);
                }
                else
                {
                    Console.WriteLine("The number of hosts exceeds the maximum limit for a Class A network.");
                }
            }
            else if (firstOctet >= 128 && firstOctet < 192)
            {
                if (sumAllHosts <= Math.Pow(2, 16))
                {
                    InjectDataToDictionary(networkIp, lengthOfSubnets, subnets);
                }
                else
                {
                    Console.WriteLine("The number of hosts exceeds the maximum limit for a Class B network.");
                }
            }
            else if (firstOctet >= 192 && firstOctet < 224)
            {
                if (sumAllHosts <= Math.Pow(2, 8))
                {
                    InjectDataToDictionary(networkIp, lengthOfSubnets, subnets);
                }
                else
                {
                    Console.WriteLine("The number of hosts exceeds the maximum limit for a Class C network.");
                }
            }
        }
        else
        {
            if (sumAllHosts <= Math.Pow(2, 32 - int.Parse(prefix)))
            {
                InjectDataToDictionary(networkIp, lengthOfSubnets, subnets);
            }
            else
            {
                Console.WriteLine("The number of hosts exceeds the maximum limit for the specified prefix length.");
            }
        }

        return subnets;
    }

    static void InjectDataToDictionary(string networkIp, List<int> lengthOfSubnets, List<Dictionary<string, object>> subnets)
    {
        foreach (int subnetSize in lengthOfSubnets)
        {
            int hostBits = (int)Math.Log2(subnetSize - 2);
            int subnetPrefix = 32 - hostBits;
            string mask = GetMaskFromPrefix(subnetPrefix);

            Dictionary<string, object> subnetData = new Dictionary<string, object>
            {
                { "Network Address", networkIp },
                { "IP Range", $"{GetFirstAddressableIp(networkIp)} - {GetLastAddressableIp(networkIp, subnetSize)}" },
                { "Broadcast Address", GetBroadcastIp(networkIp, subnetSize) },
                { "Subnet Mask", mask },
                { "Prefix", $"/{subnetPrefix}" },
                { "Addressable Hosts", subnetSize - 2 }
            };

            // Get the IP address of the next network
            networkIp = GetNextNetworkIp(networkIp, subnetSize);

            // Append the subnet information to the list
            subnets.Add(subnetData);
        }
    }

    static void Main()
    {
        // Take user inputs
        Console.Write("Enter the initial network address: (xxx.xxx.xxx.xxx");
        string networkIp = Console.ReadLine();

        Console.Write("Enter the number of hosts per network: (x,x,x,x.....)");
        string endpointNumbersPerNetwork = Console.ReadLine();

        Console.Write("Enter the subnet mask prefix (leave blank for the default based on the network address): ");
        string prefix = Console.ReadLine();

        // Check if user inputs are valid
        if (IsCorrectNetworkAddress(networkIp) && IsCorrectEndpointNumbersPerNetwork(endpointNumbersPerNetwork))
        {
            // Calculate VLSM
            List<Dictionary<string, object>> subnets = CalculateVlsm(networkIp, endpointNumbersPerNetwork, prefix);

            foreach (Dictionary<string, object> subnet in subnets)
            {
                Console.WriteLine($"\nSubnet Information:");
                Console.WriteLine($"Network Address: {subnet["Network Address"]}");
                Console.WriteLine($"Prefix: {subnet["Prefix"]}");
                Console.WriteLine($"IP Range: {subnet["IP Range"]}");
                Console.WriteLine($"Broadcast Address: {subnet["Broadcast Address"]}");
                Console.WriteLine($"Subnet Mask: {subnet["Subnet Mask"]}");
                Console.WriteLine($"Addressable Hosts: {subnet["Addressable Hosts"]}\n");
            }
        }
        else
        {
            Console.WriteLine("Invalid input.");
        }
    }
}
