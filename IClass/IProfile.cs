using System;
using System.Data;
                       
namespace RFID.Utility.IClass
{	

	public interface IReadOnlyProfile : ICloneable {

		string Name { get; }
		
		object GetValue(string section, string entry);		
		string GetValue(string section, string entry, string defaultValue);		
		int GetValue(string section, string entry, int defaultValue);		
		double GetValue(string section, string entry, double defaultValue);		
		bool GetValue(string section, string entry, bool defaultValue);		
		bool HasEntry(string section, string entry);		
		bool HasSection(string section);		
		string[] GetEntryNames(string section);		
		string[] GetSectionNames();
		DataSet GetDataSet();
	}

	public enum ProfileChangeType {	Name, ReadOnly, SetValue, RemoveEntry, RemoveSection, Other }
	public class ProfileChangedArgs : EventArgs {
		// Fields
		private readonly ProfileChangeType m_changeType;
		private readonly string m_section;
		private readonly string m_entry;
		private readonly object m_value;

		public ProfileChangedArgs(ProfileChangeType changeType, string section, string entry, object value) {
			m_changeType = changeType;
			m_section = section;
			m_entry = entry;
			m_value = value;
		}

		public ProfileChangeType ChangeType {
			get { return m_changeType; }
		}
	
		public string Section {
			get { return m_section;}
		}

		public string Entry {
			get { return m_entry; }
		}

		public object Value {
			get { return m_value; }
		}
	}

	public class ProfileChangingArgs : ProfileChangedArgs {
		private bool m_cancel;
		
		public ProfileChangingArgs(ProfileChangeType changeType, string section, string entry, object value) :
			base(changeType, section, entry, value) {}

		
		public bool Cancel {
			get { return m_cancel; }
			set { m_cancel = value; }
		}
	}
	
	public delegate void ProfileChangingHandler(object sender, ProfileChangingArgs e);
	public delegate void ProfileChangedHandler(object sender, ProfileChangedArgs e);

	public interface IProfile : IReadOnlyProfile {		
		new string Name { get; set; }		
		string DefaultName { get; }	
		bool ReadOnly { get; set; }				
		void SetValue(string section, string entry, object value);			
		void RemoveEntry(string section, string entry);	
		void RemoveSection(string section);		
		void SetDataSet(DataSet ds);	
		IReadOnlyProfile CloneReadOnly();	
		event ProfileChangingHandler Changing;
		event ProfileChangedHandler Changed;				
	}
}

