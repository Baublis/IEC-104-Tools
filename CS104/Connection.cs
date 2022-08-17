/*
 *  Connection.cs
 *
 *  Copyright 2016, 2017 MZ Automation GmbH
 *
 *  This file is part of lib60870.NET
 *
 *  lib60870.NET is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  lib60870.NET is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with lib60870.NET.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  See COPYING file for the complete license text.
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using lib60870.CS101;
using lib60870.CS101.InformationObjects;

namespace lib60870.CS104
{
    /// <summary>
    /// События соединения для клиента CS 104
    /// </summary>
    public enum ConnectionEvent
    {
		/// <summary>
		/// Соединение было открыто
		/// </summary>
        OPENED = 0,

        /// <summary>
        /// Соединение закрыто
        /// </summary>
        I_CLOSED = 1,

        /// <summary>
        /// Попытка соединения не удалась
        /// </summary>
        CONNECT_FAILED = 2,

        /// <summary>
        /// Извещение об отправке START_DT ACT
        /// </summary>
        STARTDT_ACT_SENDED = 3,

        /// <summary>
        /// Получен START DT (сервер будет отправлять и принимать сообщения прикладного уровня)
        /// </summary>
        STARTDT_ACT_RECEIVED = 4,

        /// <summary>
        /// Извещение об отправке START_DT CON (отправлен ответ на принятый START_DT ACT)
        /// </summary>
        STARTDT_CON_SENDED = 5,

        /// <summary>
        /// Получен START DT CON (сервер будет отправлять и принимать сообщения прикладного уровня)
        /// </summary>
        STARTDT_CON_RECEIVED = 6,

        /// <summary>
        /// Извещение об отправке STOP_DT ACT (отправлено сообщение STOP_DT ACT)
        /// Клиент больше не будет отправлять и принимать сообщения прикладного уровня
        /// </summary>
        STOPDT_ACT_SENDED = 7,

        /// <summary>
        /// Получен STOP_DT ACT
        /// Клиент больше не будет отправлять и принимать сообщения прикладного уровня
        /// </summary>
        STOPDT_ACT_RECEIVED = 8,

        /// <summary>
        /// Извещение об отправке STOP_DT CON (отправлен ответ на принятый STOP_DT ACT)
        /// </summary>
        STOPDT_CON_SENDED = 9,

        /// <summary>
        /// Получен STOP_DT CON (получен ответ на отправленный STOP_DT ACT)
        /// Сервер больше не будет отправлять и принимать сообщения прикладного уровня
        /// </summary>
        STOPDT_CON_RECEIVED = 10,

        /// <summary>
        /// Извещение об отправке TEST_FR ACT (отправлено тестовое сообщение TEST_FR ACT)
        /// </summary>
        TESTFR_ACT_SENDED = 11,

        /// <summary>
        /// Получен TEST_FR ACT (получено тестовое сообщение TEST_FR ACT)
        /// </summary>
        TESTFR_ACT_RECEIVED = 12,

        /// <summary>
        /// Извещение об отправке TEST_FR CON (отправлен ответ на принятое тестовое сообщение TEST_FR ACT)
        /// </summary>
        TESTFR_CON_SENDED = 13,

        /// <summary>
        /// Получен TEST_FR CON (получен ответ на отправленное тестовое сообщение TEST_FR ACT)
        /// </summary>
        TESTFR_CON_RECEIVED = 14,

        /// <summary>
        /// Соединение закрыто удаленным компьютером
        /// </summary>
        SERV_CLOSED = 15,

        /// <summary>
        /// Соединение сброшено удаленным компьютером
        /// </summary>
        SERV_RESET = 16,

        /// <summary>
        /// Соединение закрыто по не получению подверждения (Т1)
        /// </summary>
        CLOSED_T1 = 17,

        /// <summary>
        /// Соединение закрыто по не получению TF_CON (T3)
        /// </summary>
        CLOSED_T3 = 18,

        /// <summary>
        /// Отправлена пустая команда подтверрждения
        /// </summary>
        SEND_S = 19,

        /// <summary>
        /// Получена пустая команда подтверрждения
        /// </summary>
        RECEIV_S = 20
    }

    /// <summary>
    /// Предоставляет некоторую статистику подключения.
    /// </summary>
    public class ConnectionStatistics
    {
        private int unconfirmedSendSequenceCounter = 0;
        private int гnconfirmedReceiveSequenceCounter = 0;
        private int sentMsgCounter = 0;
        private int rcvdMsgCounter = 0;
        private int rcvdTestFrActCounter = 0;
        private int rcvdTestFrConCounter = 0;     
        private int sendSequenceNumber = 0;
        private int receiveSequenceNumber = 0;
        private UInt64 timerT1Counter = 0;
        private UInt64 timerT2Counter = 0;
        private UInt64 timerT3Counter = 0;
        internal void Reset()
        {
            unconfirmedSendSequenceCounter = 0;
            гnconfirmedReceiveSequenceCounter = 0;
            sentMsgCounter = 0;
            rcvdMsgCounter = 0;
            rcvdTestFrActCounter = 0;
            rcvdTestFrConCounter = 0;
            sendSequenceNumber = 0;
            receiveSequenceNumber = 0;
        }

        /// <summary>
        /// Счетчик отправленных сообщений
        /// </summary>
        public int SentMsgCounter
        {
            get
            {
                return this.sentMsgCounter;
            }
            internal set
            {
                this.sentMsgCounter = value;
            }
        }

		/// <summary>
		/// Счетчик полученных сообщений
		/// </summary>
        public int RcvdMsgCounter
        {
            get
            {
                return this.rcvdMsgCounter;
            }
            internal set
            {
                this.rcvdMsgCounter = value;
            }
        }

        /// <summary>
        /// Счетчик полученных TEST_FR ACT
        /// </summary>
        public int RcvdTestFrActCounter
        {
            get
            {
                return this.rcvdTestFrActCounter;
            }
            internal set
            {
                this.rcvdTestFrActCounter = value;
            }
        }

        /// <summary>
        /// Счетчик полученных TEST_FR CON
        /// </summary>
        public int RcvdTestFrConCounter
        {
            get
            {
                return this.rcvdTestFrConCounter;
            }
            internal set
            {
                this.rcvdTestFrConCounter = value;
            }
        }

        /// <summary>
        /// Счетчик отправленных неподтвержденных ASDU
        /// </summary>
        public int UnconfirmedSendSequenceCounter
        {
           get
            {
                return this.unconfirmedSendSequenceCounter;
            }
           internal set
            {
                this.unconfirmedSendSequenceCounter = value;
            }
        }

        /// <summary>
        /// Счетчик полученных неподтвержденных ASDU
        /// </summary>
        public int UnconfirmedReceiveSequenceCounter
        {
            get
            {
                return this.гnconfirmedReceiveSequenceCounter;
            }
            internal set
            {
                this.гnconfirmedReceiveSequenceCounter = value;
            }
        }

        /// <summary>
        /// Счетчик отправленных ASDU - N(S).
        /// </summary>
        public int SendSequenceCounter
        {
            get
            {
                return this.sendSequenceNumber;
            }
            set
            {
                this.sendSequenceNumber = value;
            }
        }

        /// <summary>
        /// Счетчик полученных ASDU - N(R).
        /// </summary>
        public int ReceiveSequenceCounter
        {
            get
            {
                return this.receiveSequenceNumber;
            }
            set
            {
                this.receiveSequenceNumber = value;
            }
        }

        public UInt64 TimerT1Counter
        {
            get
            {
                return this.timerT1Counter;
            }
            set
            {
                this.timerT1Counter = value;
            }
        }

        public UInt64 TimerT2Counter
        {
            get
            {
                return this.timerT2Counter;
            }
            set
            {
                this.timerT2Counter = value;
            }
        }

        public UInt64 TimerT3Counter
        {
            get
            {
                return this.timerT3Counter;
            }
            set
            {
                this.timerT3Counter = value;
            }
        }
    }

    /// <summary>
    /// Обработчик полученных ASDU
    /// </summary>
    public delegate bool ASDUReceivedHandler(object parameter, ASDU asdu);

    /// <summary>
    /// Обработчик отправленных ASDU
    /// </summary>
    public delegate bool ASDUSentedHandler(object parameter, ASDU asdu);

    /// <summary>
    /// Обрабочик событий соединения
    /// </summary>
    public delegate void ConnectionHandler(object parameter, ConnectionEvent connectionEvent);

    /// <summary>
    /// Обрабочик всех сообщений и исключений. Для отладки
    /// </summary>
    public delegate void DebugLogHandler(string message);

    /// <summary>
    /// Одиночное подключение CS 104 (IEC 60870-5-104). Основной интерфейс
    /// </summary>
    public class Connection : Master
    {
        static byte[] STARTDT_ACT_MSG = new byte[] { 0x68, 0x04, 0x07, 0x00, 0x00, 0x00 };

        static byte[] STARTDT_CON_MSG = new byte[] { 0x68, 0x04, 0x0b, 0x00, 0x00, 0x00 };

        static byte[] STOPDT_ACT_MSG = new byte[] { 0x68, 0x04, 0x13, 0x00, 0x00, 0x00 };

        static byte[] STOPDT_CON_MSG = new byte[] { 0x68, 0x04, 0x23, 0x00, 0x00, 0x00 };

        static byte[] TESTFR_ACT_MSG = new byte[] { 0x68, 0x04, 0x43, 0x00, 0x00, 0x00 };

        static byte[] TESTFR_CON_MSG = new byte[] { 0x68, 0x04, 0x83, 0x00, 0x00, 0x00 };


        private UInt64 uMessageTimeout = 0;

        /**********************************************/

        /// <summary>
        /// Структура, которая формирует очередь для отпрвки ASDU (k - buffer)
        /// </summary>
        private struct SentASDU
        {
            /// <summary>
            /// Текущее время, когда сформирована ASDU. Точка отчета для тайм-аута T1
            /// </summary>
            public long sentTime;

            /// <summary>
            /// Номер отправляемой ASDU 
            /// </summary>
            public int seqNo;
        }

        /// <summary>
        /// Максимальное количество отправленных ASDU, которое не требует подверждение (параметр k)
        /// </summary>
        private int maxSentASDUs;

        /// <summary>
        /// Индекс посредней ASDU, зашедшей в k-buffer
        /// </summary>
        private int oldestSentASDU = -1;

        /// <summary>
        /// Индекс посредней ASDU, зашедшей в k-buffer
        /// </summary>
        private int newestSentASDU = -1;

        /// <summary>
        /// Определение k-buffer
        /// </summary>
        private SentASDU[] sentASDUs = null; 

        /**********************************************/

        private bool checkSequenceNumbers = true;

        private Queue<ASDU> waitingToBeSent = null;
        private bool useSendMessageQueue = true;        /* определяет существование очереди отправки сообщений (k - buffer) */

        private UInt64 nextT3Timeout;
        private int outStandingTestFRConMessages = 0;

        private Thread workerThread = null;

        private long lastConfirmationTime;              /* метка времени, когда было отправлено последнее подтверждающее сообщение */
        private bool timeoutT2Triggered = false;

        private bool fullbuf_K = false;
        public bool fullbuf_K_out => fullbuf_K;

        private Socket socket = null;
        private Stream netStream = null;
        private TlsSecurityInformation tlsSecInfo = null;

        private bool autostart = true;

		private FileClient fileClient = null;

        private string hostname;
        protected int tcpPort;

        private bool running = false;
        public bool connecting = false;

        private bool auto = false;

        public bool socketError;

        private SocketException lastException;

        private static int connectionCounter = 0;
        private int connectionID;

        private APCIParameters apciParameters;
        private ApplicationLayerParameters alParameters;

        /// <summary>
        /// Получает или задает значение, указывающее, следует ли использовать очередь для отправки сообщений .
        /// </summary>
        /// <description>
        /// Если <c>true</c>, то Connection формирует очередь отправки ASDU
        /// Это может случится, если приемная сторона (slave/server) не может обработать сообщение или ведомое устройство
        /// не получило подверждение ASDU. 
        /// Если <c>false</c>, то новые ASDU будут игнорироваться.
        /// </description>
        /// <value><c>true</c> если использовать очередь отправки сообщений; иначе, <c>false</c>.</value>
        public bool UseSendMessageQueue
        {
            get
            {
                return this.useSendMessageQueue;
            }
            set
            {
                useSendMessageQueue = value;
            }
        }

        protected bool CheckSequenceNumbers
        {
            get
            {
                return checkSequenceNumbers;
            }
            set
            {
                checkSequenceNumbers = value;
            }
        }

        /// <summary>
        /// Получает или задает значение, указывающее, будет ли автоматичеки отправлено
        /// сообщение STARTDT_ACT при запуске.
        /// </summary>
        /// <value><c>true</c> - отправлять сообщение STARTDT_ACT при запуске; иначе, <c>false</c>.</value>
        public bool Autostart
        {
            get
            {
                return this.autostart;
            }
            set
            {
                this.autostart = value;
            }
        }

        private void DebugLog(string message)
        {
            if (debuglogHandler != null)
                debuglogHandler(message);
            if (debugOutput)
                Console.WriteLine("CS104 MASTER CONNECTION " + connectionID + ": " + message);
        }

        public ConnectionStatistics statistics = new ConnectionStatistics();

        private void ResetConnection()
        {
            lastConfirmationTime = System.Int64.MaxValue;
            timeoutT2Triggered = false;
            outStandingTestFRConMessages = 0;

            uMessageTimeout = 0;

            socketError = false;
            lastException = null;

            maxSentASDUs = apciParameters.K;
            oldestSentASDU = -1;
            newestSentASDU = -1;
            sentASDUs = new SentASDU[maxSentASDUs];

            if (useSendMessageQueue)
                waitingToBeSent = new Queue<ASDU>();

            statistics.Reset();
        }

        private int connectTimeoutInMs = 1000;

        public ApplicationLayerParameters Parameters
        {
            get
            {
                return this.alParameters;
            }
        }

        private ASDUSentedHandler asduSentedHandler = null;
		private object asduSentedHandlerParameter = null;

        private ASDUReceivedHandler asduReceivedHandler = null;
        private object asduReceivedHandlerParameter = null;

        private ConnectionHandler connectionHandler = null;
        private object connectionHandlerParameter = null;

		private RawMessageHandler recvRawMessageHandler = null;
		private object recvRawMessageHandlerParameter = null;

		private RawMessageHandler sentMessageHandler = null;
		private object sentMessageHandlerParameter = null;

        private DebugLogHandler debuglogHandler = null;

        /// <summary>
        /// Отправка пустого сообщения подверждения (S сообщение)
        /// </summary>
        private void SendSMessage(string massage_S)
        {           
            byte[] msg = new byte[6];

            msg[0] = 0x68;
            msg[1] = 0x04;
            msg[2] = 0x01;
            msg[3] = 0;
            msg[4] = (byte)((statistics.ReceiveSequenceCounter % 128) * 2);
            msg[5] = (byte)(statistics.ReceiveSequenceCounter / 128);

            netStream.Write(msg, 0, msg.Length);

            statistics.SentMsgCounter++;

            if (sentMessageHandler != null)
            {
                sentMessageHandler(sentMessageHandlerParameter, msg, 6);
            }
            if (connectionHandler != null)
                connectionHandler(massage_S ?? "", ConnectionEvent.SEND_S);
        }

        private bool CheckSequenceNumber(int seqNo)
        {

            if (checkSequenceNumbers)
            {

                lock (sentASDUs)
                {

                    /* проверить, действителен ли полученный порядковый номер */

                    bool seqNoIsValid = false;
                    bool counterOverflowDetected = false;

                    if (oldestSentASDU == -1)
                    { /* если k-буфер пуст */
                        if (seqNo == statistics.SendSequenceCounter)
                            seqNoIsValid = true;
                    }
                    else
                    {
                        if (sentASDUs[oldestSentASDU].seqNo <= sentASDUs[newestSentASDU].seqNo)
                        {
                            if ((seqNo >= sentASDUs[oldestSentASDU].seqNo) &&
                                (seqNo <= sentASDUs[newestSentASDU].seqNo))
                                seqNoIsValid = true;

                        }
                        else
                        {
                            if ((seqNo >= sentASDUs[oldestSentASDU].seqNo) ||
                                (seqNo <= sentASDUs[newestSentASDU].seqNo))
                                seqNoIsValid = true;

                            counterOverflowDetected = true;
                        }

                        int latestValidSeqNo = (sentASDUs[oldestSentASDU].seqNo - 1) % 32768;

                        if (latestValidSeqNo == seqNo)
                            seqNoIsValid = true;
                    }

                    if (seqNoIsValid == false)
                    {
                        DebugLog("Полученный порядковый номер вне допустимого диапазона");
                        return false;
                    }

                    if (oldestSentASDU != -1)
                    {
                        do
                        {
                            if (counterOverflowDetected == false)
                            {
                                if (seqNo < sentASDUs[oldestSentASDU].seqNo)
                                    break;
                            }
                            else
                            {
                                if (seqNo == ((sentASDUs[oldestSentASDU].seqNo - 1) % 32768))
                                    break;
                            }

                            oldestSentASDU = (oldestSentASDU + 1) % maxSentASDUs;

                            int checkIndex = (newestSentASDU + 1) % maxSentASDUs;

                            if (oldestSentASDU == checkIndex)
                            {
                                oldestSentASDU = -1;
                                break;
                            }

                            if (sentASDUs[oldestSentASDU].seqNo == seqNo)
                                break;

                        } while (true);
                    }
                }
            }

            return true;
        }

        private bool IsSentBufferFull()
        {

            if (oldestSentASDU == -1)
                return false;

            //int newIndex = (newestSentASDU + 1) % maxSentASDUs;

            if (statistics.UnconfirmedSendSequenceCounter >= maxSentASDUs)
            {
                fullbuf_K = true;
                return true;
            }
            else
            {
                fullbuf_K = false;
                return false;
            }              
        }

        private int SendIMessage(ASDU asdu)
        {
            BufferFrame frame = new BufferFrame(new byte[260], 6); /* reserve space for ACPI */
            asdu.Encode(frame, alParameters);

            byte[] buffer = frame.GetBuffer();

            int msgSize = frame.GetMsgSize(); /* ACPI + ASDU */

            buffer[0] = 0x68;

            /* set size field */
            buffer[1] = (byte)(msgSize - 2);

            buffer[2] = (byte)((statistics.SendSequenceCounter % 128) * 2);
            buffer[3] = (byte)(statistics.SendSequenceCounter / 128);

            buffer[4] = (byte)((statistics.ReceiveSequenceCounter % 128) * 2);
            buffer[5] = (byte)(statistics.ReceiveSequenceCounter / 128);

            if (running)
            {
                netStream.Write(buffer, 0, msgSize);

                statistics.SendSequenceCounter = (statistics.SendSequenceCounter + 1) % 32768;
                statistics.SentMsgCounter++;

                statistics.UnconfirmedReceiveSequenceCounter = 0;
                timeoutT2Triggered = false;

                if (sentMessageHandler != null)
                {
                    sentMessageHandler(statistics.SendSequenceCounter, buffer, msgSize);
                }

                //return statistics.SendSequenceNumber;
            }
            else
            {
                if (lastException != null)
                {
                    DebugLog(lastException.Message);
                    throw new ConnectionException(lastException.Message, lastException);
                }                  
                else
                {
                    DebugLog("Связь потерена");
                    throw new ConnectionException("not connected", new SocketException(10057));
                }                
            }
            return statistics.SendSequenceCounter;
        }

        private void PrintSendBuffer()
        {

            if (oldestSentASDU != -1)
            {

                int currentIndex = oldestSentASDU;

                int nextIndex = 0;

                DebugLog("------k-buffer------");

                do
                {
                    DebugLog(currentIndex + " : S " + sentASDUs[currentIndex].seqNo + " : time " +
                        sentASDUs[currentIndex].sentTime);

                    if (currentIndex == newestSentASDU)
                        nextIndex = -1;

                    currentIndex = (currentIndex + 1) % maxSentASDUs;

                } while (nextIndex != -1);

                DebugLog("--------------------");

            }
        }

        private void SendIMessageAndUpdateSentASDUs(ASDU asdu)
        {          
            lock (sentASDUs)
            {

                int currentIndex = 0;

                if (oldestSentASDU == -1)
                {
                    oldestSentASDU = 0;
                    newestSentASDU = 0;
                }
                else
                {
                    currentIndex = (newestSentASDU + 1) % maxSentASDUs;
                    
                }
                

                sentASDUs[currentIndex].seqNo = SendIMessage(asdu);
                sentASDUs[currentIndex].sentTime = SystemUtils.currentTimeMillis();

                newestSentASDU = currentIndex;


                PrintSendBuffer();
                if (asduSentedHandler != null)
                    asduSentedHandler(statistics.SendSequenceCounter, asdu);
            }
        }

        private bool SendNextWaitingASDU()
        {
            bool sentAsdu = false;

            if (running == false)
            {
                DebugLog("Связь потерена");
                throw new ConnectionException("connection lost");
            }
            else

            try
            {

                lock (waitingToBeSent)
                {

                    while (waitingToBeSent.Count > 0)
                    {

                        if (IsSentBufferFull() == true)
                        {
                            break;
                        }
                        else if(statistics.UnconfirmedSendSequenceCounter < maxSentASDUs)
                            statistics.UnconfirmedSendSequenceCounter++;

                        ASDU asdu = waitingToBeSent.Dequeue();

                        if (asdu != null)
                        {
                            SendIMessageAndUpdateSentASDUs(asdu);
                            sentAsdu = true;                         
                        }
                        else
                            break;

                    }
                }
            }
            catch (Exception)
            {
                running = false;
                DebugLog("Связь потерена");
                throw new ConnectionException("connection lost");
            }

            return sentAsdu;
        }

        private void SendASDUInternal(ASDU asdu)
        {
            lock (socket)
            {

                if (running == false)
                {
                    DebugLog("Связь потерена");
                    throw new ConnectionException("Связь потерена", new SocketException(10057));
                }                                       
                else
                {
                    if (useSendMessageQueue)
                    {
                        lock (waitingToBeSent)
                        {
                            waitingToBeSent.Enqueue(asdu);
                        }

                        SendNextWaitingASDU();
                    }
                    else
                    {

                        if (IsSentBufferFull())
                        {
                            DebugLog("Перегрузка управления потоком. Попробуйте позже.");
                            throw new ConnectionException("Перегрузка управления потоком. Попробуйте позже.");
                        }   
                        else
                            SendIMessageAndUpdateSentASDUs(asdu);
                    }
                }
                          
            }
        }

        private void Setup(string hostname, APCIParameters apciParameters, ApplicationLayerParameters alParameters, int tcpPort)
        {
            this.hostname = hostname;
            this.alParameters = alParameters;
            this.apciParameters = apciParameters;
            this.tcpPort = tcpPort;
            this.connectTimeoutInMs = apciParameters.T0 * 1000;

            connectionCounter++;
            connectionID = connectionCounter;
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="lib60870.CS104.Connection"/> class.
		/// </summary>
		/// <param name="hostname">hostname of IP address of the CS 104 server</param>
		/// <param name="tcpPort">TCP port of the CS 104 server</param>
        public Connection(string hostname, int tcpPort = 2404)
        {
            Setup(hostname, new APCIParameters(), new ApplicationLayerParameters(), tcpPort);
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="lib60870.CS104.Connection"/> class.
		/// </summary>
		/// <param name="hostname">hostname of IP address of the CS 104 server</param>
		/// <param name="apciParameters">APCI parameters.</param>
		/// <param name="alParameters">application layer parameters.</param>
        public Connection(string hostname, APCIParameters apciParameters, ApplicationLayerParameters alParameters)
        {
            Setup(hostname, apciParameters.Clone(), alParameters.Clone(), 2404);
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="lib60870.CS104.Connection"/> class.
		/// </summary>
		/// <param name="hostname">hostname of IP address of the CS 104 server</param>
		/// <param name="tcpPort">TCP port of the CS 104 server</param>
		/// <param name="apciParameters">APCI parameters.</param>
		/// <param name="alParameters">application layer parameters.</param>
        public Connection(string hostname, int tcpPort, APCIParameters apciParameters, ApplicationLayerParameters alParameters)
        {
            Setup(hostname, apciParameters.Clone(), alParameters.Clone(), tcpPort);
        }

        /// <summary>
        /// Set the security parameters for TLS
        /// </summary>
        /// <param name="securityInfo">Security info.</param>
        public void SetTlsSecurity(TlsSecurityInformation securityInfo)
        {
            tlsSecInfo = securityInfo;

            if (securityInfo != null)
                this.tcpPort = 19998;
        }

		/// <summary>
		/// Gets the conenction statistics.
		/// </summary>
		/// <returns>The connection statistics.</returns>
        public ConnectionStatistics GetStatistics()
        {
            return this.statistics;
        }

		/// <summary>
		/// Sets the connect timeout
		/// </summary>
		/// <param name="millies">timeout value in milliseconds (ms)</param>
        public void SetConnectTimeout(int millies)
        {
            this.connectTimeoutInMs = millies;
        }

        /// <summary>
        /// Отправляет команду опроса (C_IC_NA_1 typeID: 100)
        /// </summary>
        /// <param name="cot">Причина передачи</param>
        /// <param name="ca">Номер станции</param>
        /// <param name="qoi">Указатель опроса (20 = опрос станции)</param>
        /// <exception cref="ConnectionException">description</exception>
        public override void SendInterrogationCommand(CauseOfTransmission cot, int ca, byte qoi)
        {
            ASDU asdu = new ASDU(alParameters, cot, false, false, (byte)alParameters.OA, ca, false);

            asdu.AddInformationObject(new InterrogationCommand(0, qoi));

            SendASDUInternal(asdu);
        }

        /// <summary>
        /// Отправляет команду опроса счетчиков (C_CI_NA_1 typeID: 101)
        /// </summary>
        /// <param name="cot">Причина передачи</param>
        /// <param name="ca">Номер станции</param>
        /// <param name="qcc">Указатель команды опроса счетчика</param>
        /// <exception cref="ConnectionException">description</exception>
        public override void SendCounterInterrogationCommand(CauseOfTransmission cot, int ca, byte qcc)
        {
            ASDU asdu = new ASDU(alParameters, cot, false, false, (byte)alParameters.OA, ca, false);

            asdu.AddInformationObject(new CounterInterrogationCommand(0, qcc));

            SendASDUInternal(asdu);
        }

        /// <summary>
        /// Отправляет команду считывания (C_RD_NA_1 typeID: 102).
        /// </summary>
        /// 
        /// Это отправит команду чтения C_RD_NA_1 (102) на ведомую / удаленную станцию. Причина передачи (СОТ) всегда = REQUEST (5).
        /// Он используется для реализации циклического опроса данных
        /// 
        /// <param name="ca">Номер станции</param>
        /// <param name="ioa">Адрес объекта информации</param>
        /// <exception cref="ConnectionException">description</exception>
        public override void SendReadCommand(int ca, int ioa)
        {
            ASDU asdu = new ASDU(alParameters, CauseOfTransmission.REQUEST, false, false, (byte)alParameters.OA, ca, false);

            asdu.AddInformationObject(new ReadCommand(ioa));

            SendASDUInternal(asdu);
        }

        /// <summary>
        /// Отправляет команду синхронизации времени (C_CS_NA_1 typeID: 103).
        /// </summary>
        /// <param name="ca">Номер станции</param>
        /// <param name="time">Выбранное время</param>
        /// <exception cref="ConnectionException">description</exception>
        public override void SendClockSyncCommand(int ca, CP56Time2a time)
        {
            ASDU asdu = new ASDU(alParameters, CauseOfTransmission.ACTIVATION, false, false, (byte)alParameters.OA, ca, false);

            asdu.AddInformationObject(new ClockSynchronizationCommand(0, time));

            SendASDUInternal(asdu);
        }

        /// <summary>
        /// Sends a test command (C_TS_NA_1 typeID: 104).
        /// </summary>
        /// 
        /// Not required and supported by IEC 60870-5-104. 
        /// 
        /// <param name="ca">Номер станции</param>
        /// <exception cref="ConnectionException">description</exception>
        public override void SendTestCommand(int ca)
        {
            ASDU asdu = new ASDU(alParameters, CauseOfTransmission.ACTIVATION, false, false, (byte)alParameters.OA, ca, false);

            asdu.AddInformationObject(new TestCommand());

            SendASDUInternal(asdu);
        }

        /// <summary>
        /// Отправляет тестовую команду с меткой времни CP56Time2a (C_TS_TA_1 typeID: 107).
        /// </summary>
        /// <param name="ca">Номер станции</param>
        /// <param name="tsc">test sequence number</param>
        /// <param name="time">test timestamp</param>
        /// <exception cref="ConnectionException">description</exception>
		public override void SendTestCommandWithCP56Time2a(int ca, ushort tsc, CP56Time2a time)
        {
            ASDU asdu = new ASDU(alParameters, CauseOfTransmission.ACTIVATION, false, false, (byte)alParameters.OA, ca, false);

            asdu.AddInformationObject(new TestCommandWithCP56Time2a(tsc, time));

            SendASDUInternal(asdu);
        }

        /// <summary>
        /// Отправляет команду установки процесса в исходное состояние (C_RP_NA_1 typeID: 105).
        /// </summary>
        /// <param name="cot">Причина передачи</param>
        /// <param name="ca">Номер станции</param>
        /// <param name="qrp">Указатель команды установки процесса в исходное состояние</param>
        /// <exception cref="ConnectionException">description</exception>
		public override void SendResetProcessCommand(CauseOfTransmission cot, int ca, byte qrp)
        {
            ASDU asdu = new ASDU(alParameters, CauseOfTransmission.ACTIVATION, false, false, (byte)alParameters.OA, ca, false);

            asdu.AddInformationObject(new ResetProcessCommand(0, qrp));

            SendASDUInternal(asdu);
        }


        /// <summary>
        /// Sends a delay acquisition command (C_CD_NA_1 typeID: 106).
        /// </summary>
        /// <param name="cot">Cause of transmission</param>
        /// <param name="ca">Номер станции</param>
        /// <param name="delay">delay for acquisition</param>
        /// <exception cref="ConnectionException">description</exception>
        public override void SendDelayAcquisitionCommand(CauseOfTransmission cot, int ca, CP16Time2a delay)
        {
            ASDU asdu = new ASDU(alParameters, CauseOfTransmission.ACTIVATION, false, false, (byte)alParameters.OA, ca, false);

            asdu.AddInformationObject(new DelayAcquisitionCommand(0, delay));

            SendASDUInternal(asdu);
        }

        /// <summary>
        /// Отправляет управляющую команду.
        /// </summary>
        /// 
        /// Идентификатор типа должен соответствовать типу информационного объекта!
        /// 
        /// C_SC_NA_1 -> SingleCommand
        /// C_DC_NA_1 -> DoubleCommand
        /// C_RC_NA_1 -> StepCommand
        /// C_SC_TA_1 -> SingleCommandWithCP56Time2a
        /// C_SE_NA_1 -> SetpointCommandNormalized
        /// C_SE_NB_1 -> SetpointCommandScaled
        /// C_SE_NC_1 -> SetpointCommandShort
        /// C_BO_NA_1 -> Bitstring32Command
        /// 
        /// <param name="cot">Причина передачи (используйте АКТИВАЦИЮ для запуска последовательности управления)</param>
        /// <param name="ca">Номер станции</param>
        /// <param name="sc">Информационный объект команды</param>
        /// <exception cref="ConnectionException">description</exception>
        public override void SendControlCommand(CauseOfTransmission cot, int ca, InformationObject sc)
        {

            ASDU controlCommand = new ASDU(alParameters, cot, false, false, (byte)alParameters.OA, ca, false);

            controlCommand.AddInformationObject(sc);

            SendASDUInternal(controlCommand);
        }

        /// <summary>
        /// Отправляет параметр.
        /// </summary>
        /// 
        /// Идентификатор типа должен соответствовать типу информационного объекта!
        /// 
        /// P_ME_NA_1 -> ParameterNormalizedValue
        /// P_ME_NB_1 -> ParameterScaledValue
        /// P_ME_NC_1 -> ParameterFloatValue
        /// P_AC_NA_1 -> ParameterActivation
        /// 
        /// <param name="cot">Причина передачи</param>
        /// <param name="ca">Номер станции</param>
        /// <param name="sc">Информационный объект команды</param>
        /// <exception cref="ConnectionException">description</exception>
        public override void SendParameters(CauseOfTransmission cot, int ca, InformationObject sc)
        {

            ASDU controlCommand = new ASDU(alParameters, cot, false, false, (byte)alParameters.OA, ca, false);

            controlCommand.AddInformationObject(sc);

            SendASDUInternal(controlCommand);
        }

        /// <summary>
        /// Отправляет ASDU подчиненному устойству
        /// </summary>
        /// <param name="asdu">ASDU для передачи</param>
        public override void SendASDU(ASDU asdu)
        {
            SendASDUInternal(asdu);         
        }

        public override ApplicationLayerParameters GetApplicationLayerParameters()
        {
            return alParameters;
        }

        /// <summary>
        /// Начать передачу данных по этому соединению. Отправить STARTDT_ACT
        /// </summary>
        public void SendStartDT_ACT()
        {
            if (running)
            {
                netStream.Write(STARTDT_ACT_MSG, 0, STARTDT_ACT_MSG.Length);

                statistics.SentMsgCounter++;

                sentMessageHandler?.Invoke(sentMessageHandlerParameter, STARTDT_ACT_MSG, 6);

                connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.STARTDT_ACT_SENDED);
            }
        }

        /// <summary>
        /// Остановить передачу данных по этому соединению. Отправить STOPDT_ACT
        /// </summary>
        public void SendStopDT_ACT()
        {
            if (running)
            {
                netStream.Write(STOPDT_ACT_MSG, 0, STOPDT_ACT_MSG.Length);

                statistics.SentMsgCounter++;

                sentMessageHandler?.Invoke(sentMessageHandlerParameter, STOPDT_ACT_MSG, 6);

                connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.STOPDT_ACT_SENDED);
            }
        }

        public void SendStartDT_CON()
        {
            if (running)
            {
                netStream.Write(STARTDT_CON_MSG, 0, STARTDT_CON_MSG.Length);

                statistics.SentMsgCounter++;

                sentMessageHandler?.Invoke(sentMessageHandlerParameter, STARTDT_CON_MSG, 6);

                connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.STARTDT_CON_SENDED);
            }
        }

        public void SendStopDT_CON()
        {
            if (running)
            {
                netStream.Write(STOPDT_CON_MSG, 0, STOPDT_CON_MSG.Length);

                statistics.SentMsgCounter++;

                sentMessageHandler?.Invoke(sentMessageHandlerParameter, STOPDT_CON_MSG, 6);

                connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.STOPDT_CON_SENDED);
            }
        }

        public void SendTestFR_ACT()
        {
            if (running)
            {
                UInt64 currentTime = (UInt64)SystemUtils.currentTimeMillis();

                netStream.Write(TESTFR_ACT_MSG, 0, TESTFR_ACT_MSG.Length);

                statistics.SentMsgCounter++;

                uMessageTimeout = (UInt64)currentTime + (UInt64)(apciParameters.T1 * 1000);

                outStandingTestFRConMessages++;

                sentMessageHandler?.Invoke(sentMessageHandlerParameter, TESTFR_ACT_MSG, 6);

                connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.TESTFR_ACT_SENDED);
            }
        }

        public void SendTestFR_CON()
        {
            if (running)
            {
                netStream.Write(TESTFR_CON_MSG, 0, TESTFR_CON_MSG.Length);

                statistics.SentMsgCounter++;

                sentMessageHandler?.Invoke(sentMessageHandlerParameter, TESTFR_CON_MSG, 6);

                connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.TESTFR_CON_SENDED);
            }          
        }


        /// <summary>
        /// Connect this instance.
        /// </summary>
        /// 
        /// The function will throw a SocketException if the connection attempt is rejected or timed out.
        /// <exception cref="ConnectionException">description</exception>
        public void Connect()
        {

            
            ConnectAsync();

            while ((running == false) && (socketError == false))
            {
                Thread.Sleep(10);
            }          
            if (socketError)
            {
                DebugLog(lastException.Message);
                throw new ConnectionException(lastException.Message, lastException);
            }
                
        }

        public void ReConnect()
        {
            ConnectAsync();
            while ((running == false) && (socketError == false))
            {
                Thread.Sleep(1);
            }
        }

        private void ResetT3Timeout()
        {
            nextT3Timeout = (UInt64)SystemUtils.currentTimeMillis() + (UInt64)(apciParameters.T3 * 1000);
        }

        /// <summary>
        /// Connects to the server (outstation). This is a non-blocking call. Before using the connection
        /// you have to check if the connection is already connected and running.
        /// </summary>
        /// <exception cref="ConnectionException">description</exception>
        public void ConnectAsync()
        {
            if ((running == false) && (connecting == false))
            {               
                ResetConnection();

                ResetT3Timeout();

                workerThread = new Thread(HandleConnection);

                workerThread.Start();

                auto = true;
            }
            else
            {
                if (running)
                {
                    DebugLog("Неудачная попытка соединения. Соединение было устнаолвено раньше.");
                    throw new ConnectionException("already connected", new SocketException(10056)); 
                }                   
                else
                {
                    DebugLog("Неудачная попытка соединения. Соединение устанавливется.");
                    throw new ConnectionException("already connecting", new SocketException(10037)); 
                }                
            }
        }

        private int receiveMessage(byte[] buffer)
        {
            int readLength = 0;

            //if (netStream.DataAvailable) {

            if (socket.Poll(50, SelectMode.SelectRead))
            {
                // wait for first byte
                if (netStream.Read(buffer, 0, 1) != 1)
                    return -1;

                if (buffer[0] != 0x68)
                {
                    DebugLog("Missing SOF indicator!");

                    return -1;
                }

                // read length byte
                if (netStream.Read(buffer, 1, 1) != 1)
                    return -1;

                int length = buffer[1];

                // read remaining frame
                if (netStream.Read(buffer, 2, length) != length)
                {
                    DebugLog("Failed to read complete frame!");

                    return -1;
                }

                readLength = length + 2;
            }

            return readLength;
        }

        private bool checkConfirmTimeout(long currentTime)
        {
            if ((currentTime - lastConfirmationTime) >= (apciParameters.T2 * 1000))
            {
                //statistics.TimerT2Counter = (UInt64)(apciParameters.T2 * 1000);
                return true;
            }             
            else
            {
                statistics.TimerT2Counter = (UInt64)(apciParameters.T2 * 1000 - currentTime + lastConfirmationTime);
                return false;
            }             
        }

        /// <summary>
        /// Проверка сообщения
        /// </summary>
        private bool checkMessage(byte[] buffer, int msgSize)
        {
            long currentTime = SystemUtils.currentTimeMillis();

            /* I format frame */
            if ((buffer[2] & 1) == 0)
            {              
                if (timeoutT2Triggered == false)
                {
                    timeoutT2Triggered = true;
                    lastConfirmationTime = currentTime; /* Запуск timeout T2 */
                }

                if (msgSize < 7)
                {
                    //DebugLog("I msg too small!");
                    return false;
                }

                int frameSendSequenceNumber = ((buffer[3] * 0x100) + (buffer[2] & 0xfe)) / 2;
                int frameRecvSequenceNumber = ((buffer[5] * 0x100) + (buffer[4] & 0xfe)) / 2;
                statistics.UnconfirmedSendSequenceCounter=0;

                //DebugLog("Received I frame: N(S) = " + frameSendSequenceNumber + " N(R) = " + frameRecvSequenceNumber);

                /* проверка порядкового номера приема N(R) - соединение будет закрыто при неожиданном значении */
                if (frameSendSequenceNumber != statistics.ReceiveSequenceCounter)
                {      
                    //DebugLog("Sequence error: Close connection!");
                    return false;
                }

                if (CheckSequenceNumber(frameRecvSequenceNumber) == false)
                    return false;

                statistics.ReceiveSequenceCounter = (statistics.ReceiveSequenceCounter + 1) % 32768;
                statistics.UnconfirmedReceiveSequenceCounter++;

                try
                {
                    Thread.Sleep(10);
                    ASDU asdu = new ASDU(alParameters, buffer, 6, msgSize);

					bool messageHandled = false;


					if (fileClient != null)
						messageHandled = fileClient.HandleFileAsdu(asdu);

					if (messageHandled == false) {

	                    if (asduReceivedHandler != null)
	                        asduReceivedHandler(statistics.UnconfirmedReceiveSequenceCounter, asdu);

					}
                }
                catch (ASDUParsingException e)
                {
                    DebugLog("ASDU parsing failed: " + e.Message);
                    return false;
                }

            }
            /* S format frame */
            else if ((buffer[2] & 0x03) == 0x01)
            { 
                int seqNo = (buffer[4] + buffer[5] * 0x100) / 2;
                statistics.UnconfirmedSendSequenceCounter = 0;

                //DebugLog("Recv S(" + seqNo + ") (own sendcounter = " + statistics.SendSequenceNumber + ")");

                if (connectionHandler != null)
                    connectionHandler(seqNo, ConnectionEvent.RECEIV_S);
                if (CheckSequenceNumber(seqNo) == false)
                    return false;

                uMessageTimeout = 0;
            }
            /* U format frame */
            else if ((buffer[2] & 0x03) == 0x03)
            {               
                if (buffer[2] == 0x43)
                { 
                    statistics.RcvdTestFrActCounter++;
                    DebugLog("RCVD TESTFR_ACT");

                    if (connectionHandler != null)
                    {
                        connectionHandler(connectionHandlerParameter, ConnectionEvent.TESTFR_ACT_RECEIVED);
                    }                      
                }
                else if (buffer[2] == 0x83)
                { /* Пришел TESTFR_CON */
                    DebugLog("RCVD TESTFR_CON");
                    statistics.RcvdTestFrConCounter++;
                    outStandingTestFRConMessages = 0;
                    if (connectionHandler != null)
                        connectionHandler(connectionHandlerParameter, ConnectionEvent.TESTFR_CON_RECEIVED);
                }
                else if (buffer[2] == 0x07)
                { /* Пришел STARTDT ACT */
                    DebugLog("RCVD STARTDT_ACT");

                    netStream.Write(STARTDT_CON_MSG, 0, STARTDT_CON_MSG.Length);

                    statistics.SentMsgCounter++;
                    if (sentMessageHandler != null)
                    {
                        sentMessageHandler(sentMessageHandlerParameter, STARTDT_CON_MSG, 6);
                    }
                    if (connectionHandler != null)
                        connectionHandler(connectionHandlerParameter, ConnectionEvent.STARTDT_ACT_RECEIVED);
                }
                else if (buffer[2] == 0x0b)
                { /* Пришел STARTDT_CON */
                    DebugLog("RCVD STARTDT_CON");

                    if (connectionHandler != null)
                        connectionHandler(connectionHandlerParameter, ConnectionEvent.STARTDT_CON_RECEIVED);

                }
                else if (buffer[2] == 0x23)
                { /* Пришел STOPDT_CON */
                    DebugLog("RCVD STOPDT_CON");

                    if (connectionHandler != null)
                        connectionHandler(connectionHandlerParameter, ConnectionEvent.STOPDT_CON_RECEIVED);
                }

                uMessageTimeout = 0;
            }
            /* Unknown message type */
            else
            {
                //DebugLog("Unknown message type");
                return false;
            }
            ResetT3Timeout();
            return true;
        }

        private bool isConnected()
        {
            try
            {
                byte[] tmp = new byte[1];


                socket.Send(tmp, 0, 0);
                return true;
            }
            catch (SocketException e)
            {
                DebugLog(e.Message);
                if (e.NativeErrorCode.Equals(10035))
                {
                    DebugLog("Подключен, но блокируется отпрвка");
                    return true;
                }
                else
                {
                    DebugLog("Не соединение: код ошибки " + e.NativeErrorCode);
                    return false;
                }
            }
        }

        private void ConnectSocketWithTimeout()
        {
            IPAddress ipAddress = null;
            IPEndPoint remoteEP = null;

            try
            {
                ipAddress = IPAddress.Parse(hostname);
                remoteEP = new IPEndPoint(ipAddress, tcpPort);              
            }
            catch (Exception)
            {
                DebugLog("Не допустимый аргумент для создания сокета");
                throw new SocketException(87); 
            }

            // Создание TCP/IP сокета.
            socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            var result = socket.BeginConnect(remoteEP, null, null);

            bool success = result.AsyncWaitHandle.WaitOne(connectTimeoutInMs, true);
            if (success)
            {
                try
                {
                    socket.EndConnect(result);
                    socket.NoDelay = true;
                    netStream = new NetworkStream(socket);               
                }
                catch (ObjectDisposedException)
                {
                    socket = null;
                    DebugLog("Socket был заянят");
                    throw new SocketException(995); 
                }
            }
            else
            {
                socket.Close();
                socket = null;

                DebugLog("Истекло время ожидания попытки подключения, или произошел сбой при отклике подключенного узла.");
                throw new SocketException(10060); 
            }
        }

        /// <summary>
        /// Проверка таймаутов
        /// </summary>
        private bool handleTimeouts()
        {
            UInt64 currentTime = (UInt64)SystemUtils.currentTimeMillis();

            if (currentTime > nextT3Timeout)
            {
                if (outStandingTestFRConMessages > 2)
                {                  
                    connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.CLOSED_T3);              
                    return false;
                }
                else
                {
                    netStream.Write(TESTFR_ACT_MSG, 0, TESTFR_ACT_MSG.Length);
                    statistics.SentMsgCounter++;                
                    uMessageTimeout = (UInt64)currentTime + (UInt64)(apciParameters.T1 * 1000);
                    outStandingTestFRConMessages++;

                    ResetT3Timeout();

                    sentMessageHandler?.Invoke(sentMessageHandlerParameter, TESTFR_ACT_MSG, 6);
                    connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.TESTFR_ACT_SENDED);

                    DebugLog("S message T3 timeout");
                }
            }
            else
                statistics.TimerT3Counter = nextT3Timeout - currentTime;

            if (statistics.UnconfirmedReceiveSequenceCounter > 0)
            {
                if (checkConfirmTimeout((long)currentTime))
				{
                    lastConfirmationTime = (long)currentTime;
                    statistics.UnconfirmedReceiveSequenceCounter = 0;
                    timeoutT2Triggered = false;
                    SendSMessage(" Таймаут Т2");
                }
            }

            if (uMessageTimeout != 0)
            {
                if (currentTime > uMessageTimeout)
                {
                    statistics.TimerT1Counter = (UInt64)(apciParameters.T1 * 1000);

                    DebugLog("S message T1 timeout");

                    connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.CLOSED_T1);

                    return false;
                }
                else
                {
                    statistics.TimerT1Counter = uMessageTimeout - currentTime;
                }                  
            }


            /* проверить, подтвердил ли контрагент сообщения */
            lock (sentASDUs)
            {
                if (oldestSentASDU != -1)
                {
                    if (((long)currentTime - sentASDUs[oldestSentASDU].sentTime) >= (apciParameters.T1 * 1000))
                    {
                        DebugLog("U message T1 timeout");

                        connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.CLOSED_T1);

                        return false;
                    }
                    else
                    {
                        statistics.TimerT1Counter = (UInt64)(apciParameters.T1 * 1000) - ((UInt64)currentTime - (UInt64)sentASDUs[oldestSentASDU].sentTime);
                    }
                }
            }

            return true;
        }

        private bool AreByteArraysEqual(byte[] array1, byte[] array2)
        {
            if (array1.Length == array2.Length)
            {

                for (int i = 0; i < array1.Length; i++)
                {
                    if (array1[i] != array2[i])
                        return false;
                }

                return true;
            }
            else
                return false;
        }

        private bool CertificateValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (certificate != null)
            {

                if (tlsSecInfo.ChainValidation)
                {

                    X509Chain newChain = new X509Chain();

                    newChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    newChain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    newChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    newChain.ChainPolicy.VerificationTime = DateTime.Now;

                    foreach (X509Certificate2 caCert in tlsSecInfo.CaCertificates)
                        newChain.ChainPolicy.ExtraStore.Add(caCert);

                    bool certificateStatus = newChain.Build(new X509Certificate2(certificate.GetRawCertData()));

                    if (certificateStatus == false)
                        return false;
                }

                if (tlsSecInfo.AllowOnlySpecificCertificates)
                {

                    foreach (X509Certificate2 allowedCert in tlsSecInfo.AllowedCertificates)
                    {
                        if (AreByteArraysEqual(allowedCert.GetCertHash(), certificate.GetCertHash()))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return true;
            }
            else
                return false;
        }

        public X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return localCertificates[0];
        }

        /// <summary>
        /// Проверка связи
        /// </summary>
        private void HandleConnection()
        {
            
            byte[] bytes = new byte[300];

            try
            {

                try
                {                    
                    connecting = true;
                    
                    try
                    {
                        
                        // Connect to a remote device.
                        ConnectSocketWithTimeout();

                        connectionHandler?.Invoke("Соединение установлено с " + socket.RemoteEndPoint.ToString(), ConnectionEvent.OPENED);

                        if (tlsSecInfo != null)
                        {

                            //DebugLog("Setup TLS");

                            SslStream sslStream = new SslStream(netStream, true, CertificateValidation, LocalCertificateSelectionCallback);

                            var clientCertificateCollection = new X509Certificate2Collection(tlsSecInfo.OwnCertificate);

                            try
                            {
								string targetHostName = tlsSecInfo.TargetHostName;

								if (targetHostName == null)
									targetHostName = "*";

								sslStream.AuthenticateAsClient(targetHostName, clientCertificateCollection, System.Security.Authentication.SslProtocols.Tls, false);
                            }
                            catch (IOException e)
                            {

                                //Console.WriteLine(e.ToString());
                                //Console.WriteLine(e.StackTrace);

                                string message;

                                if (e.GetBaseException() != null)
                                {
                                    message = e.GetBaseException().Message;
                                }
                                else
                                {
                                    message = e.Message;
                                }

                                //DebugLog("TLS authentication error: " + message);
                              
                                DebugLog("Истекло время ожидания попытки подключения, или произошел сбой при отклике подключенного узла.");
                                throw new SocketException(10060);
                            }

                            if (sslStream.IsAuthenticated)
                            {
                                netStream = sslStream;
                            }
                            else
                            {
                                DebugLog("Истекло время ожидания попытки подключения, или произошел сбой при отклике подключенного узла.");
                                throw new SocketException(10060);
                            }

                        }

                        netStream.ReadTimeout = 50;                  

                        if (autostart)
                        {
                            netStream.Write(STARTDT_ACT_MSG, 0, STARTDT_ACT_MSG.Length);
                            statistics.SentMsgCounter++;                         
                            if (connectionHandler != null)
                                connectionHandler(connectionHandlerParameter, ConnectionEvent.STARTDT_ACT_SENDED);
                            if (sentMessageHandler != null)                         
                                sentMessageHandler(sentMessageHandlerParameter, STARTDT_ACT_MSG, 6);                          
                        }

                        running = true;
                        socketError = false;
                        connecting = false;                  
                    }
                    catch (SocketException se)
                    {
                        //DebugLog("SocketException: " + se.ToString());

                        running = false;
                        socketError = true;
                        lastException = se;

                        if (connectionHandler != null)
                            connectionHandler(connectionHandlerParameter, ConnectionEvent.CONNECT_FAILED);
                    }

                    if (running)
                    {

                        bool loopRunning = running;

                        while (loopRunning)
                        {

                            bool suspendThread = true;

                            try
                            {
                                // Получение сообщения от удаленного устройства.
                                int bytesRec = receiveMessage(bytes);

                                if (bytesRec > 0)
                                {

                                    //DebugLog("RCVD: " + BitConverter.ToString(bytes, 0, bytesRec));

                                    statistics.RcvdMsgCounter++;

                                    bool handleMessage = true;

                                    if (recvRawMessageHandler != null)
                                        handleMessage = recvRawMessageHandler(recvRawMessageHandlerParameter, bytes, bytesRec);

                                    if (handleMessage)
                                    {
                                        if (checkMessage(bytes, bytesRec) == false)
                                        {
                                            // Закрыть соединение при ошибке                                                                   
                                            loopRunning = false;
                                        }
                                    }

                                    if (statistics.UnconfirmedReceiveSequenceCounter >= apciParameters.W)
                                    {
                                        lastConfirmationTime = SystemUtils.currentTimeMillis();
                                        statistics.UnconfirmedReceiveSequenceCounter = 0;
                                        timeoutT2Triggered = false;
                                        SendSMessage(" Лимит W");
                                    }
                                    suspendThread = false;
                                }
                                else if (bytesRec == -1)
                                {
                                    connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.SERV_CLOSED);
                                    loopRunning = false;
                                }
                                    
                                if (handleTimeouts() == false)
                                {                                   
                                    loopRunning = false;
                                }


                                if (isConnected() == false)
                                {
                                    loopRunning = false;
                                }
                                    
                                if (useSendMessageQueue)
                                {
                                    if (SendNextWaitingASDU() == true)
                                        suspendThread = false;
                                }

                                if (suspendThread)
                                {
                                    Thread.Sleep(10);
                                }                                 

                            }
                            catch (SocketException b)
                            {
                                DebugLog("SocketException: " + b.ToString());
                                loopRunning = false;
                                if (connectionHandler != null && auto)
                                    connectionHandler(connectionHandlerParameter, ConnectionEvent.SERV_RESET);
                            }
                            catch (IOException e)
                            {
                                if (connectionHandler != null && auto)
                                    connectionHandler(connectionHandlerParameter, ConnectionEvent.SERV_RESET);
                                DebugLog(e.InnerException.Message.ToString());
                                loopRunning = false;
                            }
                            catch (ConnectionException c)
                            {
                                DebugLog("ConnectionException: " + c.ToString());
                                loopRunning = false;
                                if (connectionHandler != null && auto)
                                    connectionHandler(connectionHandlerParameter, ConnectionEvent.SERV_RESET);
                            }
                        }
                        DebugLog("CLOSE CONNECTION!");                       
                        // Release the socket.
                        try
                        {
                            socket.Shutdown(SocketShutdown.Both);
                        }
                        catch (SocketException)
                        {

                        }

                        socket.Close();

                        netStream.Dispose();

                        //connectionHandler?.Invoke(connectionHandlerParameter, ConnectionEvent.I_CLOSED);
                    }

                }
                catch (ArgumentNullException ane)
                {
                    connecting = false;
                    DebugLog("ArgumentNullException: " + ane.ToString());
                }
                catch (SocketException se)
                {
                    DebugLog("SocketException: " + se.ToString());
                }
                catch (Exception e)
                {
                    DebugLog("Unexpected exception: " + e.ToString());
                }

            }
            catch (Exception e)
            {
               DebugLog(e.ToString());
            }    


            running = false;
            connecting = false;
        }

        public bool IsRunning
        {
            get
            {
                return this.running;
            }
        }

        public void Cancel(int time)
        {       
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                socket.Close(time);
            }

        }

        public void AutoClose()
        {         
            if (running)
            {                
                running = false;
                workerThread.Join();
            }
        }

        public void ManualCancel(int time)
        {
            auto = false;
            if (connectionHandler != null)
                connectionHandler(connectionHandlerParameter, ConnectionEvent.I_CLOSED);
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                socket.Close();
                netStream.Dispose();
            }
        }

        /// <summary>
        /// Set the ASDUReceivedHandler. This handler is invoked whenever a new ASDU is received
        /// </summary>
        /// <param name="handler">the handler to be called</param>
        /// <param name="parameter">user provided parameter that is passed to the handler</param>
		public  void SetASDUReceivedHandler(ASDUReceivedHandler handler, object parameter)
        {
            asduReceivedHandler = handler;
            asduReceivedHandlerParameter = parameter;
        }

        public void SetASDUSentedHandler(ASDUSentedHandler handler, object parameter)
        {
            asduSentedHandler = handler;
            asduSentedHandlerParameter = parameter;
        }

        /// <summary>
        /// Sets the connection handler. The connection handler is called when
        /// the connection is established or closed
        /// </summary>
        /// <param name="handler">the handler to be called</param>
        /// <param name="parameter">user provided parameter that is passed to the handler</param>
        public void SetConnectionHandler(ConnectionHandler handler, object parameter)
        {
            connectionHandler = handler;
            connectionHandlerParameter = parameter;
        }

        public void SetDebugLogHandler(DebugLogHandler handler)
        {
            debuglogHandler = handler;
        }

        /// <summary>
        /// Sets the raw message handler. This is a callback to intercept raw messages received.
        /// </summary>
        /// <param name="handler">Handler/delegate that will be invoked when a message is received</param>
        /// <param name="parameter">will be passed to the delegate</param>
		public override void SetReceivedRawMessageHandler(RawMessageHandler handler, object parameter)
        {
            recvRawMessageHandler = handler;
            recvRawMessageHandlerParameter = parameter;
        }

        /// <summary>
        /// Устанавливает обработчик отправленного сообщения. Перехват сообщений
        /// </summary>
        /// <param name="handler">Обработчик/делегат вызывамый при отправе сообщений<</param>
        /// <param name="parameter">Будет передан в обработчик/делегат</param>
		public override void SetSentRawMessageHandler(RawMessageHandler handler, object parameter)
        {
            sentMessageHandler = handler;
            sentMessageHandlerParameter = parameter;
        }

        /// <summary>
        /// Определяет, заполнен ли буфер передачи (отправки). Если true, следующая команда отправки вызовет исключение ConnectionException.
        /// </summary>
        /// <returns><c>true</c> если буфер заполнен; иначе, <c>false</c>.</returns>
        public bool IsTransmitBufferFull()
        {
            if (useSendMessageQueue)
                return false;
            else
                return IsSentBufferFull();
        }

		public override void GetFile(int ca, int ioa, NameOfFile nof, IFileReceiver receiver)
		{
			if (fileClient == null)
				fileClient = new FileClient (this, DebugLog);

			fileClient.RequestFile (ca, ioa, nof, receiver);
		}

		public void GetDirectory(int ca) 
		{
			ASDU getDirectoryAsdu = new ASDU (GetApplicationLayerParameters (), CauseOfTransmission.REQUEST, false, false, 0, ca, false);

			InformationObject io = new FileCallOrSelect (0, NameOfFile.DEFAULT, 0, SelectAndCallQualifier.DEFAULT);

			getDirectoryAsdu.AddInformationObject (io);

			SendASDU (getDirectoryAsdu);
		}
    }
}

