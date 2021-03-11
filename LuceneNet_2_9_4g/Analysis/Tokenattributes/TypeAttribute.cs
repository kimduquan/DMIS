/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Attribute = Lucene.Net.Util.IAttribute;

namespace Lucene.Net.Analysis.Tokenattributes
{
	
	/// <summary> A Token's lexical type. The Default value is "word". </summary>
	public interface TypeAttribute:Attribute
	{
		/// <summary>Returns this Token's lexical type.  Defaults to "word". </summary>
		System.String Type();
		
		/// <summary>Set the lexical type.</summary>
		/// <seealso cref="Type()">
		/// </seealso>
		void  SetType(System.String type);
	}
}