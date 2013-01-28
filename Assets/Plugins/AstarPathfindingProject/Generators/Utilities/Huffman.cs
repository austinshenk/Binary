using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

//namespace ENusbaum.Compression
//{
    
    /** Class for encoding strings or byte arrays with Huffman encoding.
      * \author Eric Nusbaum (eric@enusbaum.com)
      * \version 1.0
      * \date 05/22/2009
      */
    public sealed class Huffman : IDisposable
    {
        #region --[ Members ]--
        /*
         * Members contains all the private and internal variables
         * used by the Huffman class to both Encode and Decode data
         */
        private List<Leaf> HuffmanTree = new List<Leaf>();
        private MemoryStream msInputbytes = new MemoryStream();
        private StringBuilder Log = new StringBuilder();
        private bool LogEnabled = false;

        /// <summary>
        ///     Container for Leafs and Nodes within the Huffman Tree
        /// </summary>
        internal class Leaf
        {
            public Guid ID;
            public Guid ParentID;
            public bool IsNode;
            public bool Left;
            public bool Right;

            public byte ByteValue;
            public int BitValue;
            public int BitCount;
            public long FrequencyValue;

            public Leaf()
            {
                ID = Guid.NewGuid();
            }
        }
        #endregion

        #region --[ IDisposable Members ]--
        /*
         * Because the Huffman Class implements IDisposable, we need to
         * implement the Dispose method.
         */

        public void Dispose()
        {
            msInputbytes.Dispose();
            HuffmanTree = null;
            Log = null;
        }

        #endregion

        #region --[ Constructors ]--
        /*
         * The Huffman Class has several constructors to make encoding
         * data easier
         */

        /// <summary>
        ///     Constructor -- Nothing added to Frequency Table
        /// </summary>
        public Huffman()
        {
            Init();
        }

        /// <summary>
        ///     Constructor -- byte[] added to Frequency Table
        /// </summary>
        /// <param name="bInput"></param>
        public Huffman(byte[] bInput)
        {
            Init();

            //Add to Freqency Table Input Buffer
            foreach (byte b in bInput)
            {
                HuffmanTree[b].FrequencyValue++;
                msInputbytes.WriteByte(b);
            }
        }

        public Huffman(string sInput)
        {
            Init();

            //Add to Input Buffer & Frequency Table
            foreach (char c in sInput)
            {
                HuffmanTree[c].FrequencyValue++;
                msInputbytes.WriteByte((byte)c);
            }

        }
        #endregion

        #region --[ Public Methods ]--
        /*
         * Public Methods exposed in the Huffman Class
         */

        /// <summary>
        ///     Adds a Byte to the Frequency Table
        /// </summary>
        /// <param name="b"></param>
        public void Add(byte b)
        {
            HuffmanTree[b].FrequencyValue++;
            BuildTree();
        }

        /// <summary>
        ///     Adds a String to the Frequency Table
        /// </summary>
        /// <param name="s"></param>
        public void Add(string s)
        {
            foreach (char c in s)
            {
                HuffmanTree[c].FrequencyValue++;
            }
        }

        /// <summary>
        ///     Clears data entered for encoding
        /// </summary>
        public void Clear()
        {
            msInputbytes.Dispose();
            msInputbytes = new MemoryStream();
        }

        /// <summary>
        ///     Applies Huffman Encoding to the data entered
        /// </summary>
        /// <param name="OutputLog">string -- Log of Encoding, which returns the Huffman Tree Information</param>
        /// <returns>byte[] -- byte[] containing the encoded data package</returns>
        public byte[] Encode(out string OutputLog)
        {
            //Enable Logging
            LogEnabled = true;

            //Build the Tree
            BuildTree();

            //Encode the Tree
            EncodeTree();

            //Encode the Data
            byte[] bEncodedOutput = Encode();

            //Setup output params and return
            OutputLog = Log.ToString();
            return bEncodedOutput;
        }

        /// <summary>
        ///     Applies Huffman Encoding to the data entered
        /// </summary>
        /// <returns>byte[] -- byte[] containing the encoded data package</returns>
        public byte[] Encode()
        {
            //Local Variables
            Int64 iBuffer = 0;
            int iBufferCount = 0;
            int iBytesEncoded = 0;

            MemoryStream msEncodedOutput = new MemoryStream();

            //Build the Tree
            BuildTree();

            //Encode the Tree
            EncodeTree();

            //Remove Nodes (since theyre not needed once the tree is built)
            List<Leaf> OptimizedTree = new List<Leaf>(HuffmanTree);
            OptimizedTree.RemoveAll(delegate(Leaf leaf) { return leaf.IsNode; });

            //Generation Dictionary to Add to Header
            MemoryStream msEncodedHeader = new MemoryStream();
            foreach (Leaf l in OptimizedTree)
            {

                if (!l.IsNode & l.FrequencyValue > 0)
                {
                    iBuffer = l.ByteValue;
                    iBuffer <<= 8;
                    iBuffer ^= l.BitCount;
                    iBuffer <<= 48;
                    iBuffer ^= l.BitValue;
                    msEncodedHeader.Write(BitConverter.GetBytes(iBuffer), 0, 8);
                    iBytesEncoded++;
                    iBuffer = 0;
                }
            }

            //Write Final Output Size 1st
            msEncodedOutput.Write(BitConverter.GetBytes(msInputbytes.Length), 0, 8);

            //Then Write Dictionary Word Count
            msEncodedOutput.WriteByte((byte)(iBytesEncoded -1));

            //Then Write Dictionary
            msEncodedOutput.Write(msEncodedHeader.ToArray(), 0, Convert.ToInt16(msEncodedHeader.Length));

            //Pad with 3 Null

            msEncodedOutput.Write(new byte[] { 66, 67, 68 }, 0, 3);

            //Begin Writing Encoded Data Stream
            iBuffer = 0;
            iBufferCount = 0;
            foreach (byte b in msInputbytes.ToArray())
            {
                Leaf FoundLeaf = OptimizedTree[b];

                //How many bits are we adding?
                iBufferCount += FoundLeaf.BitCount;

                //Shift the buffer if it's not == 0x00
                if (iBuffer != 0)
                {
                    iBuffer <<= FoundLeaf.BitCount;
                    iBuffer ^= FoundLeaf.BitValue;
                }
                else
                {
                    iBuffer = FoundLeaf.BitValue;
                }

                //Are there at least 8 bits in the buffer?
                while (iBufferCount > 7)
                {
                    //Write to output
                    int iBufferOutput = (int)(iBuffer >> (iBufferCount - 8));
                    msEncodedOutput.WriteByte((byte)iBufferOutput);
                    iBufferCount = iBufferCount - 8;
                    iBufferOutput <<= iBufferCount;
                    iBuffer ^= iBufferOutput;
                }
            }

            //Write remaining bits in buffer
            if (iBufferCount > 0)
            {
                iBuffer = iBuffer << (8 - iBufferCount);
                msEncodedOutput.WriteByte((byte)iBuffer);
            }
            return msEncodedOutput.ToArray();
        }

        public byte[] Decode(byte[] bInput)
        {
            //Local Variables
            List<Leaf> DecodeDictionary = new List<Leaf>(255);
            Leaf DecodedLeaf = null;
            long iInputBuffer = 0;
            long iStreamLength = 0;
            int iInputBufferSize = 0;
            int iOutputBuffer = 0;
            int iDictionaryRecords = 0;
            int iDictionaryEndByte = 0;
            int iBytesWritten = 0;
            
            //Populate Decode Dictionary with 256 Leafs
            for (int i = 0; i < 256; i++)
            {
                DecodeDictionary.Add(new Leaf());
            }

            //Retrieve Stream Length
            iStreamLength = BitConverter.ToInt64(bInput, 0);
            
            //Establish Output Buffer to write unencoded data to
            byte[] bDecodedOutput = new byte[iStreamLength];

            //Retrieve Records in Dictionary
            iDictionaryRecords = bInput[8];

            //Calculate Ending Byte of Dictionary
            iDictionaryEndByte = (((iDictionaryRecords +1) * 8) + 8);

            //Begin Decoding Dictionary (4 Bytes Per Entry)
            for (int i = 9; i <= iDictionaryEndByte; i += 8)
            {
                iInputBuffer = BitConverter.ToInt64(bInput, i);

                DecodedLeaf = new Leaf();

                //Get Byte Value
                DecodedLeaf.ByteValue = (byte)(iInputBuffer >> 56);
                if(DecodedLeaf.ByteValue != 0) iInputBuffer ^= (((Int64)DecodedLeaf.ByteValue) << 56);

                //Get Bit Count
                DecodedLeaf.BitCount = (int)(iInputBuffer >> 48);
                iInputBuffer ^= (((Int64)DecodedLeaf.BitCount) << 48);

                //Get Bit Value
                DecodedLeaf.BitValue = (int)(iInputBuffer);

                //Add Decoded Leaf to Dictionary
                DecodeDictionary[DecodedLeaf.ByteValue] = DecodedLeaf;
            }

            //Begin Looping through Input and Decoding
            iInputBuffer = 0;
            for (int i = (iDictionaryEndByte +4); i < bInput.Length; i++)
            {
                //Increment the Buffer
                iInputBufferSize += 8;

                if (iInputBuffer != 0)
                {
                    iInputBuffer <<= 8;
                    iInputBuffer ^= bInput[i];
                }
                else
                {
                    iInputBuffer = bInput[i];
                }

                //Loop through the Current Buffer until it's exhausted
                for (int j = (iInputBufferSize - 1); j >= 0; j--)
                {
                    iOutputBuffer = (int)(iInputBuffer >> j);

                    //Leading 0;
                    if (iOutputBuffer == 0) continue;
                    int iBitCount = iInputBufferSize - j;
                    //Try and find a byte in the dictionary that matches what's currently in the buffer
                    for (int k = 0; k < 256; k++)
                    {
                        if (DecodeDictionary[k].BitValue == iOutputBuffer && DecodeDictionary[k].BitCount == iBitCount)
                        {
                            //Byte Found, Write it to the Output Buffer and XOR it from the current Input Buffer
                            bDecodedOutput[iBytesWritten] = DecodeDictionary[k].ByteValue;
                            iOutputBuffer <<= j;
                            iInputBuffer ^= iOutputBuffer;
                            iInputBufferSize = j;
                            iBytesWritten++;
                            break;
                        }
                    }
                }
            }
            return bDecodedOutput;
        }
        #endregion

        #region --[ Private Method ]--
        /*
         * Private Methods protected in the Huffman Class
         */

        /// <summary>
        ///     Populate the Leaf Table (Frequency Table), the Leafs will be turned into Nodes
        /// </summary>
        private void Init()
        {
            //Setup Freqency Table with Leafs
            for (short i = 0; i <= 255; i++)
            {
                HuffmanTree.Add(new Leaf() { ByteValue = (byte)i });
            }
        }

        /// <summary>
        ///     Walks up the tree from each Leaf, encoding as it goes.
        /// </summary>
        /// <returns></returns>
        private bool EncodeTree()
        {
            //StringBuilder sbOutput = new StringBuilder();
            int iBinaryValue = 0;
            int iBitCount = 0;
            //int iLeadingZeros = 0;

            //Go through the Frequency Table and create Leafs from it
            foreach (Leaf Node in HuffmanTree)
            {
                //Only process the byte if it actually occurs
                if (!Node.IsNode && Node.FrequencyValue != 0)
                {
                    iBinaryValue = 0;
                    iBitCount = 0;
                    //iLeadingZeros = 0;

                    if (Node.Left || Node.Right)
                    {
                        //Left Node == 0, Right Node == 1
                        if (Node.Left) iBitCount++;
                        else if (Node.Right)
                        {
                            iBinaryValue ^= ((int)1 << iBitCount);
                            iBitCount++;
                        }

                        //Process up the tree through the parent nodes
                        Leaf ParentNode = HuffmanTree.Find(delegate(Leaf leaf) { return leaf.ID == Node.ParentID; });
                        while (ParentNode.ParentID != new Guid())
                        {
                            //Left Node == 0, Right Node == 1
                            if (ParentNode.Left) iBitCount++;
                            else if (ParentNode.Right)
                            {
                                iBinaryValue ^= ((int)1 << iBitCount);
                                iBitCount++;
                            }

                            //Continue up the tree to the parent nodes
                            ParentNode = HuffmanTree.Find(delegate(Leaf leaf) { return leaf.ID == ParentNode.ParentID; });
                        }
                    }

                    //Account for Leading Zeros
                    //Total C# cheater way to do it, but whatever... it works :P
                    //if (iBinaryValue != 0) {
                    	//iLeadingZeros = iBitCount - Convert.ToString(iBinaryValue, 2).Length;
                    //}
                    
                    //else iLeadingZeros = iBitCount;

                    //Assign the Encoded value to the Node
                    Node.BitValue = iBinaryValue;
                    Node.BitCount = iBitCount;

                    //Make sure it's not all 0's, as this wouldn't decode correctly
                    if (Node.BitValue == 0)
                    {
                        Node.BitValue = 1;
                        Node.BitCount++;
                    }

                    //Appent it to the output log
                    if(LogEnabled) Log.AppendLine(string.Format("{0} = {1} (Bits: {2})", (int)Node.ByteValue, Convert.ToString(Node.BitValue, 2), Node.BitCount ));
                }
            }
            return true;
        }

        /// <summary>
        ///     Takes Frequency Value and Establishes Parent/Child relationship with Tree Nodes & Leafs
        /// </summary>
        /// <returns></returns>
        
        private bool BuildTree()
        {
        	Debug.Log ("Calling Build Tree");
            //Local Variables
            int iParentIndex = 0;

            List<Leaf> OptimizedTree = new List<Leaf>(HuffmanTree);
            List<Leaf> WorkingTree;
            Leaf NewParent;

            //Remove anything with a 0 Frequency Value
            OptimizedTree.RemoveAll(delegate(Leaf leaf) { return leaf.FrequencyValue == 0; });

            //Order with highest frequency at 'end', lowest at 'beginning'
            OptimizedTree.Sort(delegate(Leaf L1, Leaf L2) { return L1.FrequencyValue.CompareTo(L2.FrequencyValue); });

            WorkingTree = new List<Leaf>(OptimizedTree);
            while (WorkingTree.Count > 1)
            {
                //Sort by Frequency
                //Order with highest frequency at 'end', lowest at 'beginning'
                WorkingTree.Sort(delegate(Leaf L1, Leaf L2) { return L1.FrequencyValue.CompareTo(L2.FrequencyValue); });

                //Take 'First Two' and join them with a new node
                NewParent = new Leaf() { FrequencyValue = WorkingTree[0].FrequencyValue + WorkingTree[1].FrequencyValue, IsNode = true };

                HuffmanTree.Add(NewParent);

                //Assign Parent to Left Node
                iParentIndex = HuffmanTree.FindIndex(delegate(Leaf L1) { return L1.Equals(WorkingTree[0]); });
                HuffmanTree[iParentIndex].Left = true;
                HuffmanTree[iParentIndex].ParentID = NewParent.ID;

                //Assign Parent to Right Node
                iParentIndex = HuffmanTree.FindIndex(delegate(Leaf L1) { return L1.Equals(WorkingTree[1]); });
                HuffmanTree[iParentIndex].Right = true;
                HuffmanTree[iParentIndex].ParentID = NewParent.ID;
				
				OptimizedTree.Clear ();
				OptimizedTree.AddRange (HuffmanTree.ToArray ());
                //OptimizedTree = new List<Leaf>(HuffmanTree);

                //Remove anything with a 0 Frequency Value
                OptimizedTree.RemoveAll(delegate(Leaf leaf) { return leaf.FrequencyValue == 0; });

                //Order with highest frequency at 'end', lowest at 'beginning'
                OptimizedTree.Sort(delegate(Leaf L1, Leaf L2) { return L1.FrequencyValue.CompareTo(L2.FrequencyValue); });
				
				//Debug.Log ("While loop");
                //WorkingTree = new List<Leaf>(OptimizedTree);
				WorkingTree.Clear ();
				WorkingTree.AddRange (OptimizedTree.ToArray ());
				
                //Remove anything with a parent
                WorkingTree.RemoveAll(delegate(Leaf leaf) { return leaf.ParentID != new Guid(); });
            }

            return true;
        }
        #endregion
    }
//}
