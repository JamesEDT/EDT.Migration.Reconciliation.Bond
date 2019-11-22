using System;

namespace Edt.Bond.Migration.Reconciliation.Framework.Models.EdtDatabase
{
    public class Document
    {
        public int DocumentID { get; set; }
        public int LocationID { get; set; }
        public int BatchID { get; set; }
        public int FamilyID { get; set; }
        public int ParentID { get; set; }
        public string DocNumber { get; set; }
        public string Title { get; set; }
        public int SuperKind { get; set; }
        public string FileExtOrMsgClass { get; set; }
        public string FileName { get; set; }
        public int DocumentTypeID { get; set; }
        public int CustodianID { get; set; }
        public long Size { get; set; }
        public long SizeWithoutChildren { get; set; }
        public DateTime PrimaryDateUtc { get; set; }
        public bool IsEstimatedDate { get; set; }
        public int PrimaryDateFlags { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime ModifiedUtc { get; set; }
        public int AttachmentCount { get; set; }
        public bool IsAttachment { get; set; }
        public int AttachmentType { get; set; }
        public int ItemIndex { get; set; }
        public DateTime SentOnUtc { get; set; }
        public DateTime ReceivedOnUtc { get; set; }
        public string ConversationIndex { get; set; }
        public string ConversationIndexRoot { get; set; }
        public string EmailEntryID { get; set; }
        public string MD5 { get; set; }
        public int DuplicateStatus { get; set; }
        public int DuplicateOriginalID { get; set; }
        public string Body { get; set; }
        public int BodyFormat { get; set; }
        public byte InclusionStatus { get; set; }
        public int Statuses { get; set; }
        public byte ContainerFileStatus { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
        public int NoPages { get; set; }
        public string EndPage { get; set; }
        public string ImportedParentNumber { get; set; }
        public short WorkflowStatus { get; set; }
        public string TempFileName { get; set; }
        public string Author { get; set; }
        public string Recipients { get; set; }
        public int FolderID { get; set; }
        public string AppTitle { get; set; }
        public string Subject { get; set; }
        public string Keywords { get; set; }
        public DateTime AppCreatedUtc { get; set; }
        public DateTime AppModifiedUtc { get; set; }
        public string Application { get; set; }
        public string PdfProducer { get; set; }
        public string PdfVersion { get; set; }
        public DateTime LastPrintedUtc { get; set; }
        public string LastSavedBy { get; set; }
        public string Comments { get; set; }
        public string VersionNumber { get; set; }
        public int EditingTime { get; set; }
        public string InternetHeaders { get; set; }
        public string Category { get; set; }
        public string Company { get; set; }
        public string Manager { get; set; }
        public int RevisionNumber { get; set; }
        public int PageCount { get; set; }
        public int WordCount { get; set; }
        public string AppCreated { get; set; }
        public string AppModified { get; set; }
        public int FamilyStrategyParentID { get; set; }
        public byte FamilyStatus { get; set; }
        public long QaStatuses { get; set; }
        public string InternetMessageID { get; set; }
        public string ExtraInfo { get; set; }
        public string cc_Relevant { get; set; }
        public string cc_Privilege { get; set; }
        public string cc_PrivilegeReason { get; set; }
        public string cc_Comment { get; set; }
        public string ExhibitNumber { get; set; }
        public string PrimaryLanguage { get; set; }
        public string MessageStatus { get; set; }
    }
}