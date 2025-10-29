namespace WeddingInvitations.Api.DTOs
{
    public class TableSummaryDto
    {
        public int Id { get; set; }
        public int TableNumber { get; set; }
        public string TableName { get; set; } = string.Empty;
        public int CurrentOccupancy { get; set; }
        public int MaxCapacity { get; set; }
        public int AvailableSeats { get; set; }
        public int PercentageOccupied { get; set; }
        public bool IsFull { get; set; }
        public bool IsHonorTable { get; set; }
    }

    public class GuestAssignmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsChild { get; set; }
        public string? Notes { get; set; }
        public int FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public int? TableId { get; set; }
        public string? TableName { get; set; }
        public string Country { get; set; } = string.Empty;
    }
}