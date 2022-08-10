/*
 *  CauseOfTransmission.cs
 *
 *  Copyright 2016 MZ Automation GmbH
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
using System.Text;
using System.Threading;

namespace lib60870.CS101
{
	/// <summary>
	/// Причина передачи
	/// </summary>
	public enum CauseOfTransmission {
		/// <summary>
		/// Причина передачи 1
		/// </summary>
		PERIODIC = 1,
		/// <summary>
		/// Причина передачи 2
		/// </summary>
		BACKGROUND_SCAN = 2,
		/// <summary>
		/// Причина передачи 3
		/// </summary>
		SPONTANEOUS = 3,
		/// <summary>
		/// Причина передачи 4
		/// </summary>
		INITIALIZED = 4,
		/// <summary>
		/// Причина передачи 5
		/// </summary>
		REQUEST = 5,
		/// <summary>
		/// Причина передачи 6
		/// </summary>
		ACTIVATION = 6,
		/// <summary>
		/// Причина передачи 7
		/// </summary>
		ACTIVATION_CON = 7,
		/// <summary>
		/// Причина передачи 8
		/// </summary>
		DEACTIVATION = 8,
		/// <summary>
		/// Причина передачи 9
		/// </summary>
		DEACTIVATION_CON = 9,
		/// <summary>
		/// Причина передачи 10
		/// </summary>
		ACTIVATION_TERMINATION = 10,
		/// <summary>
		/// Причина передачи 11
		/// </summary>
		RETURN_INFO_REMOTE = 11,
		/// <summary>
		/// Причина передачи 12
		/// </summary>
		RETURN_INFO_LOCAL = 12,
		/// <summary>
		/// Причина передачи 13
		/// </summary>
		FILE_TRANSFER =	13,
		/// <summary>
		/// Причина передачи 14
		/// </summary>
		AUTHENTICATION = 14,
		/// <summary>
		/// Причина передачи 15
		/// </summary>
		MAINTENANCE_OF_AUTH_SESSION_KEY = 15,
		/// <summary>
		/// Причина передачи 16
		/// </summary>
		MAINTENANCE_OF_USER_ROLE_AND_UPDATE_KEY = 16,
		/// <summary>
		/// Причина передачи 20
		/// </summary>
		INTERROGATED_BY_STATION = 20,
		/// <summary>
		/// Причина передачи 21
		/// </summary>
		INTERROGATED_BY_GROUP_1 = 21,
		/// <summary>
		/// Причина передачи 22
		/// </summary>
		INTERROGATED_BY_GROUP_2 = 22,
		/// <summary>
		/// Причина передачи 23
		/// </summary>
		INTERROGATED_BY_GROUP_3 = 23,
		/// <summary>
		/// Причина передачи 24
		/// </summary>
		INTERROGATED_BY_GROUP_4 = 24,
		/// <summary>
		/// Причина передачи 25
		/// </summary>
		INTERROGATED_BY_GROUP_5 = 25,
		/// <summary>
		/// Причина передачи 26
		/// </summary>
		INTERROGATED_BY_GROUP_6 = 26,
		/// <summary>
		/// Причина передачи 27
		/// </summary>
		INTERROGATED_BY_GROUP_7 = 27,
		/// <summary>
		/// Причина передачи 28
		/// </summary>
		INTERROGATED_BY_GROUP_8 = 28,
		/// <summary>
		/// Причина передачи 29
		/// </summary>
		INTERROGATED_BY_GROUP_9 = 29,
		/// <summary>
		/// Причина передачи 30
		/// </summary>
		INTERROGATED_BY_GROUP_10 = 30,
		/// <summary>
		/// Причина передачи 31
		/// </summary>
		INTERROGATED_BY_GROUP_11 = 31,
		/// <summary>
		/// Причина передачи 32
		/// </summary>
		INTERROGATED_BY_GROUP_12 = 32,
		/// <summary>
		/// Причина передачи 33
		/// </summary>
		INTERROGATED_BY_GROUP_13 = 33,
		/// <summary>
		/// Причина передачи 34
		/// </summary>
		INTERROGATED_BY_GROUP_14 = 34,
		/// <summary>
		/// Причина передачи 35
		/// </summary>
		INTERROGATED_BY_GROUP_15 = 35,
		/// <summary>
		/// Причина передачи 36
		/// </summary>
		INTERROGATED_BY_GROUP_16 = 36,
		/// <summary>
		/// Причина передачи 37
		/// </summary>
		REQUESTED_BY_GENERAL_COUNTER = 37,
		/// <summary>
		/// Причина передачи 38
		/// </summary>
		REQUESTED_BY_GROUP_1_COUNTER = 38,
		/// <summary>
		/// Причина передачи 39
		/// </summary>
		REQUESTED_BY_GROUP_2_COUNTER = 39,
		/// <summary>
		/// Причина передачи 40
		/// </summary>
		REQUESTED_BY_GROUP_3_COUNTER = 40,
		/// <summary>
		/// Причина передачи 41
		/// </summary>
		REQUESTED_BY_GROUP_4_COUNTER = 41,
		/// <summary>
		/// Причина передачи 44
		/// </summary>
		UNKNOWN_TYPE_ID = 44,
		/// <summary>
		/// Причина передачи 45
		/// </summary>
		UNKNOWN_CAUSE_OF_TRANSMISSION =	45,
		/// <summary>
		/// Причина передачи 46
		/// </summary>
		UNKNOWN_COMMON_ADDRESS_OF_ASDU = 46,
		/// <summary>
		/// Причина передачи 47
		/// </summary>
		UNKNOWN_INFORMATION_OBJECT_ADDRESS = 47
	}
	
}
