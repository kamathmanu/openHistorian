#******************************************************************************************************
#  openHistorian.py - Gbtc
#
#  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
#
#  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
#  the NOTICE file distributed with this work for additional information regarding copyright ownership.
#  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
#  file except in compliance with the License. You may obtain a copy of the License at:
#
#      http://opensource.org/licenses/MIT
#
#  Unless agreed to in writing, the subject software distributed under the License is distributed on an
#  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
#  License for the specific language governing permissions and limitations.
#
#  Code Modification History:
#  ----------------------------------------------------------------------------------------------------
#  01/31/2021 - J. Ritchie Carroll
#       Generated original version of source code.
#
#******************************************************************************************************

from openHistorian.historianInstance import historianInstance
from openHistorian.historianKey import historianKey
from openHistorian.historianValue import historianValue
from openHistorian.metadataCache import metadataCache
from snapDB.snapConnection import snapConnection
from snapDB.encodingDefinition import encodingDefinition
from gsf.streamEncoder import streamEncoder
from gsf import override
from typing import Optional, Callable
from enum import IntEnum
from time import time, sleep
import errno
import socket
import gzip
import numpy as np

class ServerCommand(IntEnum):
    # Meta data refresh command.
    METADATAREFRESH = 0x01
    # Define operational modes for subscriber connection.
    DEFINEOPERATIONALMODES = 0x06

class ServerResponse(IntEnum):
    # Command succeeded response.
    SUCCEEDED = 0x80
    # Command failed response.
    FAILED = 0x81

class historianConnection(snapConnection[historianKey, historianValue]):
    """
    Defines API functionality for connecting to an openHistorian instance then
    reading and writing measurement data from the instance.

    This class is an instance of the `snapConnection` implemented for the
    openHistorian `historianKey` and `historianValue` SNAPdb types.
    """

    def __init__(self, hostAddress: str):
        super().__init__(hostAddress, historianKey(), historianValue())
        self.metadata: Optional[metadata] = None

    @override
    def OpenInstance(self, instanceName: str, definition: Optional[encodingDefinition] = None) -> historianInstance:
        return super().OpenInstance(instanceName, definition)

    @property
    def Metadata(self) -> Optional[metadataCache]:
        return self.metadata

    def RefreshMetadata(self, sttpPort: int = 7175, logOutput: Optional[Callable[[str], None]] = None) -> int:
        """
        Requests updated metadata from openHistorian connection.
        """
        sttpSocket = socket.socket(socket.AF_INET, socket.SOCK_STREAM, socket.IPPROTO_TCP)        
        socketRead = lambda length: sttpSocket.recv(length)
        socketWrite = lambda buffer: sttpSocket.send(buffer)        
        stream = streamEncoder(socketRead, socketWrite)
        
        logOutput = (lambda value: print(value)) if logOutput is None else logOutput
        logOutput("Requesting metadata from openHistorian...")
        opStart = time()

        # Using STTP connection to get metadata only (no subscription), hence the following operational modes:
        # OperationalModes.CompressMetadata | CompressionModes.GZip | OperationalEncoding.UTF8 | (OperationalModes.VersionMask & 1U) 
        operationalModes = 0x80000221

        try:
            sttpSocket.connect((self.HostIPAddress, sttpPort))

            # Establish operational modes for STTP connection
            stream.WriteInt32(5, "big") # Payload aware buffer length
            stream.WriteByte(ServerCommand.DEFINEOPERATIONALMODES)
            stream.WriteUInt32(operationalModes, "big")

            # Request metadata refresh
            stream.WriteInt32(1, "big") # Payload aware buffer length
            stream.WriteByte(ServerCommand.METADATAREFRESH)

            while True:
                # Get payload aware buffer length
                bufferLength = stream.ReadInt32("big")
                
                # Get server response
                responseCode = ServerResponse(stream.ReadByte())
                commandCode = ServerCommand(stream.ReadByte())
                length = 0 if bufferLength < 3 else stream.ReadInt32("big") 
                
                # Read response payload
                buffer = historianConnection.ReadBytes(stream, length)
                
                logOutput(f"Received {length:,} bytes of metadata in {(time() - opStart):.2f} seconds. Decompressing...")
                opStart = time()

                # Other commands can come spontaneously, like NoOp,
                # only interested in MetadataRefresh response
                if commandCode != ServerCommand.METADATAREFRESH:
                    continue
            
                if responseCode == ServerResponse.SUCCEEDED:
                    try:
                        # Decompress full metadata response XML
                        buffer = gzip.decompress(buffer)
                    except Exception as ex:
                        raise RuntimeError(f"Failed to decompress metadata: {ex}")

                    logOutput(f"Decompressed {len(buffer):,} bytes of metadata in {(time() - opStart):.2f} seconds. Parsing...")
                    opStart = time()

                    try:
                        # Parse and cache received metadata XML
                        self.metadata = metadataCache(buffer.decode("utf-8"))
                    except Exception as ex:
                        raise RuntimeError(f"Failed to parse metadata: {ex}")

                    recordCount = len(self.metadata.Records)
                    logOutput(f"Parsed metadata {recordCount:,} records in {(time() - opStart):.2f} seconds.")

                    # Return number of metadata records
                    return recordCount
                else:
                    raise RuntimeError(f"Failure code received in response to STTP metadata refresh request: {buffer.decode('utf-8')}")
        finally:
            sttpSocket.close()

    @staticmethod
    def ReadBytes(stream: streamEncoder, length: int) -> bytes:
        buffer = bytearray(length)
        position = 0

        while length > 0:
            bytesRead = stream.Read(buffer, position, length)
            
            if bytesRead == 0:
                raise RuntimeError("End of stream")

            length -= bytesRead
            position += bytesRead

        return bytes(buffer)

