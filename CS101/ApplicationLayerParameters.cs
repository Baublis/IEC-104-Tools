﻿/*
 *  ApplicationLayerParameters.cs
 *
 *  Copyright 2017 MZ Automation GmbH
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

namespace lib60870.CS101
{
	public class ApplicationLayerParameters
	{
		public static int IEC60870_5_104_MAX_ASDU_LENGTH = 249;

		private int sizeOfTypeId = 1; // По ГОСТ Р МЭК 60870-5-104-2004 используется только режим 1 (первым передается младший байт), как определено в 4.10

		private int sizeOfVSQ = 1; /* VSQ = variable sturcture qualifier */

		private int sizeOfCOT = 2; /* Размер причины передачи  (COT = cause of transmission) (1 или 2 байта) */

		private int originatorAddress = 0; // Адрес источника (OA = Originator Address) Если адрес источника не используется, то он устанаативается в 0. 

		private int sizeOfCA = 2; /* Размер общего адреса ASDU (CA = common address of ASDUs) (1 или 2 байта) */

		private int sizeOfIOA = 3; /* Размер адреса объекта информации (IOA = information object address) (1,2 или 3 байта) */

		private int maxAsduLength = IEC60870_5_104_MAX_ASDU_LENGTH; /* maximum length of ASDU */

		public ApplicationLayerParameters ()
		{
		}
	
		public ApplicationLayerParameters Clone()
		{
			ApplicationLayerParameters copy = new ApplicationLayerParameters ();

			copy.sizeOfTypeId = sizeOfTypeId;
			copy.sizeOfVSQ = sizeOfVSQ;
			copy.sizeOfCOT = sizeOfCOT;
			copy.originatorAddress = originatorAddress;
			copy.sizeOfCA = sizeOfCA;
			copy.sizeOfIOA = sizeOfIOA;
			copy.maxAsduLength = maxAsduLength;

			return copy;
		}

		public int SizeOfCOT {
			get {
				return this.sizeOfCOT;
			}
			set {
				sizeOfCOT = value;
			}
		}

		public int OA {
			get {
				return this.originatorAddress;
			}
			set {
				originatorAddress = value;
			}
		}

		public int SizeOfCA {
			get {
				return this.sizeOfCA;
			}
			set {
				sizeOfCA = value;
			}
		}

		public int SizeOfIOA {
			get {
				return this.sizeOfIOA;
			}
			set {
				sizeOfIOA = value;
			}
		}	


		public int SizeOfTypeId {
			get {
				return this.sizeOfTypeId;
			}
		}

		public int SizeOfVSQ {
			get {
				return this.sizeOfVSQ;
			}
		}

		public int MaxAsduLength {
			get {
				return this.maxAsduLength;
			}
			set {
				maxAsduLength = value;
			}
		}
	}
}
