/*
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

namespace lib60870.CS104
{
	/// <summary>
	/// Параметры связи CS 104 APCI (Application Protocol Control Information)
	/// </summary>
	public class APCIParameters
	{

		private int k = 12; /*	Количество неподтвержденных APDU
								(диапазон: 1 .. 32767 (2^15 - 1)) - отправитель будет
								останавливать передачу после k неподтвержденных сообщений */

		private int w = 8;  /*	Количество неподтвержденных APDU
								(range: 1 .. 32767 (2^15 - 1) - получатель
								подтвердит самое позднее после w неподтвержденных сообщений 
								(Рекомендация: значение w не должно быть более двух третей значения к) */

		private int t0 = 10; /* Таймаут для установления соединения (в секундах) */

		private int t1 = 15; /* Таймаут для подверждения передаваемых APDU в формате I / U (в секундах),
								по истечении тайм-аута соединение будет закрыто */

		private int t2 = 10; /* Таймаут для подверждения пустых APDU (отсутствия сообщения сданными ) 
		                        в формате I / U (в секундах), по истечении тайм-аута соединение будет закрыто 
								 t2 < t1  */

		private int t3 = 20; /* Тайм-аут для посылки блоков тестирования в случае долгого простоя */


		public APCIParameters ()
		{
		}

		public APCIParameters Clone() {
			APCIParameters copy = new APCIParameters();

			copy.k = k;
			copy.w = w;
			copy.t0 = t0;
			copy.t1 = t1;
			copy.t2 = t2;
			copy.t3 = t3;

			return copy;
		}

		public int K {
			get {
				return this.k;
			}
			set {
				k = value;
			}
		}

		public int W {
			get {
				return this.w;
			}
			set {
				w = value;
			}
		}

		public int T0 {
			get {
				return this.t0;
			}
			set {
				t0 = value;
			}
		}

		public int T1 {
			get {
				return this.t1;
			}
			set {
				t1 = value;
			}
		}

		public int T2 {
			get {
				return this.t2;
			}
			set {
				t2 = value;
			}
		}

		public int T3 {
			get {
				return this.t3;
			}
			set {
				t3 = value;
			}
		}
	}
}