﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinSound
{
    /// <summary>
    /// Buffer
    /// </summary>
    public class Buffer
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Buffer(Object sender, uint maxRTPPackets, uint timerIntervalInMilliseconds)
        {
            // Maintain minimum number
            if (maxRTPPackets < 2)
            {
                throw new Exception("Wrong Arguments. Minimum maxRTPPackets is 2");
            }

            m_Sender = sender;
            m_MaxRTPPackets = maxRTPPackets;
            m_TimerIntervalInMilliseconds = timerIntervalInMilliseconds;

            Init();
        }

        // Attribute
        private Object m_Sender = null;
        private uint m_MaxRTPPackets = 10;
        private uint m_TimerIntervalInMilliseconds = 20;
        private global::WinSound.EventTimer m_Timer = new global::WinSound.EventTimer();
        private System.Collections.Generic.Queue<RTPPacket> m_Buffer = new Queue<RTPPacket>();
        private RTPPacket m_LastRTPPacket = new RTPPacket();
        private bool m_Underflow = true;
        private bool m_Overflow = false;

        // Delegates && Event
        public delegate void DelegateDataAvailable(Object sender, RTPPacket packet);
        public event DelegateDataAvailable DataAvailable;

        /// <summary>
        /// Number of packets in the buffer
        /// </summary>
        public int Length
        {
            get
            {
                return m_Buffer.Count;
            }
        }
        /// <summary>
        /// Maximum number of RTP packets
        /// </summary>
        public uint Maximum
        {
            get
            {
                return m_MaxRTPPackets;
            }
        }
        /// <summary>
        /// Interval In Milliseconds
        /// </summary>
        public uint IntervalInMilliseconds
        {
            get
            {
                return m_TimerIntervalInMilliseconds;
            }
        }
        /// <summary>
        /// Init
        /// </summary>
        private void Init()
        {
            InitTimer();
        }
        /// <summary>
        /// InitTimer
        /// </summary>
        private void InitTimer()
        {
            m_Timer.TimerTick += new EventTimer.DelegateTimerTick(OnTimerTick);
        }
        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            m_Timer.Start(m_TimerIntervalInMilliseconds, 0);
            m_Underflow = true;
        }
        /// <summary>
        /// Stop
        /// </summary>
        public void Stop()
        {
            m_Timer.Stop();
            m_Buffer.Clear();
        }
        /// <summary>
        /// OnTimerTick
        /// </summary>
        private void OnTimerTick()
        {
            try
            {
                if (DataAvailable != null)
                {
                    // If data exists
                    if (m_Buffer.Count > 0)
                    {
                        // If overflow
                        if (m_Overflow)
                        {
                            // Wait until buffer is half empty
                            if (m_Buffer.Count <= m_MaxRTPPackets / 2)
                            {
                                m_Overflow = false;
                            }
                        }

                        // If underflow
                        if (m_Underflow)
                        {
                            // Warten bis Buffer halb - voll ist
                            if (m_Buffer.Count < m_MaxRTPPackets / 2)
                            {
                                return;
                            }
                            else
                            {
                                m_Underflow = false;
                            }
                        }

                        // Daten schicken
                        m_LastRTPPacket = m_Buffer.Dequeue();
                        DataAvailable(m_Sender, m_LastRTPPacket);
                    }
                    else
                    {
                        // Kein Overflow
                        m_Overflow = false;

                        // Wenn Buffer leer
                        if (m_LastRTPPacket != null && m_Underflow == false)
                        {
                            if (m_LastRTPPacket.Data != null)
                            {
                                // Underflow vorhanden
                                m_Underflow = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("JitterBuffer.cs | OnTimerTick() | {0}", ex.Message));
            }
        }
        /// <summary>
        /// AddData
        /// </summary>
        /// <param name="data"></param>
        public void AddData(RTPPacket packet)
        {
            try
            {
                //Wenn kein Überlauf
                if (m_Overflow == false)
                {
                    //Maximalgrösse beachten
                    if (m_Buffer.Count <= m_MaxRTPPackets)
                    {
                        m_Buffer.Enqueue(packet);
                        //m_Buffer.OrderBy(x => x.SequenceNumber);
                    }
                    else
                    {
                        //Bufferüberlauf
                        m_Overflow = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("JitterBuffer.cs | AddData() | {0}", ex.Message));
            }
        }
    }
}
